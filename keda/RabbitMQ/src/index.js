import amqplib from "amqplib";

const {
  RABBITMQ_URL = "amqp://guest:guest@localhost:5672",
  EXCHANGE_NAME = "order-events",
  EXCHANGE_TYPE = "topic",
  QUEUE_NAME = "order-processor",
  ROUTING_KEY = "#",
} = process.env;

function log(level, msg) {
  const ts = new Date().toISOString();
  console.log(`${ts} [${level}] ${msg}`);
}

function handleOrderCreated(cloudEvent) {
  const order = cloudEvent.data;

  log(
    "info",
    `[ORDER CREATED] EventId=${cloudEvent.id} | ` +
      `OrderId=${order.OrderId} | ` +
      `Customer=${order.CustomerName} (${order.CustomerId}) | ` +
      `Items=${order.Items.length} | ` +
      `Total=${order.TotalAmount.toFixed(2)} ${order.Currency} | ` +
      `CreatedAt=${order.CreatedAt}`,
  );
}

function handleMessage(msg) {
  if (!msg) return;

  try {
    const cloudEvent = JSON.parse(msg.content.toString());

    switch (cloudEvent.type) {
      case "com.ecommerce.order.created":
        handleOrderCreated(cloudEvent);
        break;
      default:
        log("warn", `No handler for event type '${cloudEvent.type}'. Skipping`);
    }
  } catch (err) {
    log("error", `Failed to parse message: ${err.message}`);
  }
}

async function main() {
  log("info", `Connecting to ${RABBITMQ_URL}...`);
  const connection = await amqplib.connect(RABBITMQ_URL);
  const channel = await connection.createChannel();

  await channel.assertExchange(EXCHANGE_NAME, EXCHANGE_TYPE, { durable: true });
  await channel.assertQueue(QUEUE_NAME, { durable: true });
  await channel.bindQueue(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY);
  await channel.prefetch(5);

  log("info", `Listening on queue '${QUEUE_NAME}' (exchange '${EXCHANGE_NAME}')`);

  channel.consume(QUEUE_NAME, (msg) => {
    handleMessage(msg);
    channel.ack(msg);
  });

  process.on("SIGTERM", async () => {
    log("info", "SIGTERM received, shutting down...");
    await channel.close();
    await connection.close();
    process.exit(0);
  });
}

main().catch((err) => {
  log("error", err.message);
  process.exit(1);
});
