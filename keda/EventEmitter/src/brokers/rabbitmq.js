import { spawn } from "child_process";
import { createConnection } from "net";
import amqplib from "amqplib";
import { serialize, CONTENT_TYPE } from "../cloud-event.js";

function isPortOpen(port, host = "127.0.0.1") {
  return new Promise((resolve) => {
    const socket = createConnection({ port, host, timeout: 500 });
    socket.on("connect", () => { socket.destroy(); resolve(true); });
    socket.on("error", () => resolve(false));
    socket.on("timeout", () => { socket.destroy(); resolve(false); });
  });
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

async function ensurePortForward(namespace = "event-processor") {
  if (await isPortOpen(5672)) return null;

  console.log("  ⎈ RabbitMQ não acessível em localhost:5672, iniciando port-forward...");

  const pf = spawn(
    "kubectl",
    ["port-forward", "-n", namespace, "svc/rabbitmq", "5672:5672"],
    { stdio: "ignore", detached: true },
  );
  pf.unref();

  for (let i = 0; i < 15; i++) {
    await sleep(1000);
    if (await isPortOpen(5672)) {
      console.log("  ⎈ Port-forward ativo (PID: " + pf.pid + ")\n");
      return pf;
    }
  }

  pf.kill();
  throw new Error("Não foi possível estabelecer port-forward para o RabbitMQ");
}

export async function send(cloudEvents, { url, topicName, exchangeType }) {
  const portForward = await ensurePortForward();

  const connection = await amqplib.connect(url);
  const channel = await connection.createConfirmChannel();

  try {
    await channel.assertExchange(topicName, exchangeType, { durable: true });

    let sentCount = 0;

    for (const event of cloudEvents) {
      const routingKey = event.type;
      const buffer = Buffer.from(serialize(event));

      channel.publish(topicName, routingKey, buffer, {
        contentType: CONTENT_TYPE,
        messageId: event.id,
        correlationId: event.data.OrderId,
        persistent: true,
      });

      sentCount++;
    }

    await channel.waitForConfirms().catch(() => {});
    return sentCount;
  } finally {
    await channel.close();
    await connection.close();
  }
}
