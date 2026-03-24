import { config } from "dotenv";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";
import { parseArgs } from "util";

const __dirname = dirname(fileURLToPath(import.meta.url));
config({ path: resolve(__dirname, "..", ".env") });

const { values: args } = parseArgs({
  options: {
    broker: { type: "string", short: "b" },
    count: { type: "string", short: "c" },
    topic: { type: "string", short: "t" },
  },
  strict: false,
});

const BROKERS = ["servicebus", "rabbitmq"];

const broker = args.broker || process.env.BROKER;
if (!broker || !BROKERS.includes(broker)) {
  console.error(
    `\n  ✗ Broker inválido: "${broker}"\n  Use: --broker ${BROKERS.join(" | ")}\n`,
  );
  process.exit(1);
}

export default Object.freeze({
  broker,
  eventCount: parseInt(args.count || process.env.EVENT_COUNT || "50", 10),
  topicName: args.topic || process.env.TOPIC_NAME || "order-events",

  servicebus: {
    connectionString: process.env.SERVICEBUS_CONNECTION_STRING,
  },

  rabbitmq: {
    url: process.env.RABBITMQ_URL || "amqp://guest:guest@localhost:5672",
    exchangeType: process.env.RABBITMQ_EXCHANGE_TYPE || "topic",
  },
});
