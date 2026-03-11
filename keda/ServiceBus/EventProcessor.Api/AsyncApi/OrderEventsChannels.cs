using EventProcessor.Api.Models;
using Saunter.Attributes;

namespace EventProcessor.Api.AsyncApi;

/// <summary>
/// Defines the AsyncAPI channels for order domain events on Azure Service Bus.
/// Messages are wrapped in a CloudEvents 1.0 envelope (structured content mode).
/// </summary>
[AsyncApi]
public class OrderEventsChannels
{
    private const string ChannelName = "order-events";
    private const string ChannelDescription =
        "Azure Service Bus topic for order domain events. " +
        "Messages use CloudEvents 1.0 structured content mode with `application/cloudevents+json` content type.";

    /// <summary>Publishes OrderCreated events to the topic.</summary>
    [Channel(ChannelName, Description = ChannelDescription)]
    [PublishOperation(
        typeof(OrderCreatedEvent),
        OperationId = "PublishOrderCreated",
        Summary = "Publish OrderCreated event",
        Description = "Emits 50 OrderCreated events wrapped as CloudEvents when the /events/publish endpoint is called.")]
    public void PublishOrderCreated(OrderCreatedEvent _) { }

    /// <summary>Consumes OrderCreated events from the subscription.</summary>
    [Channel(ChannelName, Description = ChannelDescription)]
    [SubscribeOperation(
        typeof(OrderCreatedEvent),
        OperationId = "ConsumeOrderCreated",
        Summary = "Consume OrderCreated events",
        Description = "Background worker that processes OrderCreated events from the 'order-processor' subscription.")]
    public void ConsumeOrderCreated(OrderCreatedEvent _) { }
}