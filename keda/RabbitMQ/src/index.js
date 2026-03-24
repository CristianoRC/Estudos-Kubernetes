import amqplib from "amqplib";

const {
  RABBITMQ_URL = "amqp://guest:guest@localhost:5672",
  EXCHANGE_NAME = "order-events",
  EXCHANGE_TYPE = "topic",
  QUEUE_NAME = "order-processor",
  ROUTING_KEY = "#",
  PROCESSING_DELAY_MS = "1000",
} = process.env;

const MAX_RETRIES = 15;
const BASE_DELAY_MS = 2000;

function log(level, msg) {
  const ts = new Date().toISOString();
  console.log(`${ts} [${level}] ${msg}`);
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
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

async function connect() {
  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    try {
      log("info", `Connecting to RabbitMQ (attempt ${attempt}/${MAX_RETRIES})...`);
      const connection = await amqplib.connect(RABBITMQ_URL);
      log("info", "Connected to RabbitMQ");
      return connection;
    } catch (err) {
      if (attempt === MAX_RETRIES) throw err;
      const delay = Math.min(BASE_DELAY_MS * attempt, 15000);
      log("warn", `Connection failed: ${err.message}. Retrying in ${delay / 1000}s...`);
      await sleep(delay);
    }
  }
}

async function main() {
  const connection = await connect();
  const channel = await connection.createChannel();

  await channel.assertExchange(EXCHANGE_NAME, EXCHANGE_TYPE, { durable: true });
  await channel.assertQueue(QUEUE_NAME, { durable: true });
  await channel.bindQueue(QUEUE_NAME, EXCHANGE_NAME, ROUTING_KEY);
  await channel.prefetch(1);

  log("info", `Listening on queue '${QUEUE_NAME}' (exchange '${EXCHANGE_NAME}', delay ${PROCESSING_DELAY_MS}ms)`);

  channel.consume(QUEUE_NAME, async (msg) => {
    handleMessage(msg);
    await sleep(parseInt(PROCESSING_DELAY_MS, 10));
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
