import { randomUUID } from "crypto";

const EVENT_SOURCE = "https://event-emitter/orders";

export function toCloudEvent(order) {
  return {
    specversion: "1.0",
    id: randomUUID(),
    type: "com.ecommerce.order.created",
    source: EVENT_SOURCE,
    time: new Date().toISOString(),
    datacontenttype: "application/json",
    subject: `orders/${order.OrderId}`,
    dataschema: "https://schemas.ecommerce.com/order-created/v1",
    data: order,
  };
}

export function serialize(cloudEvent) {
  return JSON.stringify(cloudEvent);
}

export const CONTENT_TYPE = "application/cloudevents+json";
