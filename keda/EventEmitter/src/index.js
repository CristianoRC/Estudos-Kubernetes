import cfg from "./config.js";
import { generateOrder } from "./order-generator.js";
import { toCloudEvent } from "./cloud-event.js";

const brokers = {
  servicebus: () => import("./brokers/servicebus.js"),
  rabbitmq: () => import("./brokers/rabbitmq.js"),
};

async function main() {
  console.log(`\n  broker:  ${cfg.broker}`);
  console.log(`  topic:   ${cfg.topicName}`);
  console.log(`  events:  ${cfg.eventCount}\n`);

  const cloudEvents = Array.from({ length: cfg.eventCount }, (_, i) =>
    toCloudEvent(generateOrder(i + 1)),
  );

  const brokerModule = await brokers[cfg.broker]();

  const brokerConfig = {
    topicName: cfg.topicName,
    ...(cfg.broker === "servicebus"
      ? { connectionString: cfg.servicebus.connectionString }
      : { url: cfg.rabbitmq.url, exchangeType: cfg.rabbitmq.exchangeType }),
  };

  const sent = await brokerModule.send(cloudEvents, brokerConfig);
  console.log(`  ✔ ${sent} eventos publicados via ${cfg.broker}\n`);
}

main().catch((err) => {
  console.error(`\n  ✗ ${err.message}\n`);
  process.exit(1);
});
