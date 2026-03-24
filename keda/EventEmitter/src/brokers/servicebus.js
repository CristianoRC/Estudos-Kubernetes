import { ServiceBusClient } from "@azure/service-bus";
import { serialize, CONTENT_TYPE } from "../cloud-event.js";

export async function send(cloudEvents, { connectionString, topicName }) {
  if (!connectionString) {
    throw new Error("SERVICEBUS_CONNECTION_STRING não configurada");
  }

  const client = new ServiceBusClient(connectionString);
  const sender = client.createSender(topicName);

  try {
    let batch = await sender.createMessageBatch();
    let sentCount = 0;

    for (const event of cloudEvents) {
      const message = {
        body: Buffer.from(serialize(event)),
        contentType: CONTENT_TYPE,
        messageId: event.id,
        correlationId: event.data.OrderId,
        subject: event.type,
      };

      if (!batch.tryAddMessage(message)) {
        await sender.sendMessages(batch);
        sentCount += batch.count;
        batch = await sender.createMessageBatch();

        if (!batch.tryAddMessage(message)) {
          throw new Error(
            `Mensagem para ${event.data.OrderId} excede o tamanho máximo do batch`,
          );
        }
      }
    }

    if (batch.count > 0) {
      await sender.sendMessages(batch);
      sentCount += batch.count;
    }

    return sentCount;
  } finally {
    await sender.close();
    await client.close();
  }
}
