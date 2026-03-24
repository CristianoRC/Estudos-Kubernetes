import amqplib from "amqplib";
import { serialize, CONTENT_TYPE } from "../cloud-event.js";

export async function send(cloudEvents, { url, topicName, exchangeType }) {
  const connection = await amqplib.connect(url);
  const channel = await connection.createChannel();

  try {
    await channel.assertExchange(topicName, exchangeType, { durable: true });
    await channel.confirmSelect();

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
