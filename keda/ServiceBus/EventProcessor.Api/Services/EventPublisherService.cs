using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using EventProcessor.Api.Models;

namespace EventProcessor.Api.Services;

public class EventPublisherService(
    ServiceBusClient serviceBusClient,
    IConfiguration configuration,
    ILogger<EventPublisherService> logger)
{
    private static readonly JsonEventFormatter CloudEventFormatter = new();
    private readonly string _topicName = configuration["ServiceBus:TopicName"]!;
    private readonly string _eventSource = configuration["ServiceBus:EventSource"]!;

    public async Task<int> PublishOrderCreatedEventsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        await using var sender = serviceBusClient.CreateSender(_topicName);

        var batch = await sender.CreateMessageBatchAsync(cancellationToken);
        var publishedCount = 0;

        logger.LogInformation("Publishing {Count} OrderCreated events to topic '{TopicName}'...", count, _topicName);

        for (var i = 1; i <= count; i++)
        {
            var order = GenerateOrder(i);
            var cloudEvent = BuildCloudEvent(order);
            var message = ToServiceBusMessage(cloudEvent, order.OrderId);

            if (!batch.TryAddMessage(message))
            {
                await sender.SendMessagesAsync(batch, cancellationToken);
                publishedCount += batch.Count;

                logger.LogDebug("Sent batch of {Count} messages, starting a new batch", batch.Count);

                batch.Dispose();
                batch = await sender.CreateMessageBatchAsync(cancellationToken);

                if (!batch.TryAddMessage(message))
                    throw new InvalidOperationException(
                        $"Message for order {order.OrderId} is too large to fit in a batch.");
            }
        }

        if (batch.Count > 0)
        {
            await sender.SendMessagesAsync(batch, cancellationToken);
            publishedCount += batch.Count;
        }

        logger.LogInformation("Successfully published {Count} OrderCreated events", publishedCount);
        return publishedCount;
    }

    private CloudEvent BuildCloudEvent(OrderCreatedEvent order)
    {
        return new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Type = "com.ecommerce.order.created",
            Source = new Uri(_eventSource),
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = order,
            ["subject"] = $"orders/{order.OrderId}",
            ["dataschema"] = new Uri("https://schemas.ecommerce.com/order-created/v1"),
        };
    }

    private static ServiceBusMessage ToServiceBusMessage(CloudEvent cloudEvent, string correlationId)
    {
        var jsonBytes = CloudEventFormatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
        var body = BinaryData.FromBytes(jsonBytes.ToArray());

        return new ServiceBusMessage(body)
        {
            ContentType = contentType.MediaType,
            MessageId = cloudEvent.Id,
            CorrelationId = correlationId,
            Subject = cloudEvent.Type,
        };
    }

    private static OrderCreatedEvent GenerateOrder(int index)
    {
        var products = new[]
        {
            ("PROD-001", "Notebook Dell XPS", 4500m),
            ("PROD-002", "Monitor Samsung 27", 1800m),
            ("PROD-003", "Teclado Mecânico Keychron", 750m),
            ("PROD-004", "Mouse Logitech MX Master", 550m),
            ("PROD-005", "Headset Sony WH-1000XM5", 2200m),
        };

        var productIndex = (index - 1) % products.Length;
        var (productId, productName, unitPrice) = products[productIndex];
        var quantity = (index % 3) + 1;

        return new OrderCreatedEvent(
            OrderId: $"ORD-{Guid.NewGuid():N}".ToUpper()[..16],
            CustomerId: $"CUST-{(index % 10) + 1:D4}",
            CustomerName: $"Customer {(index % 10) + 1}",
            TotalAmount: unitPrice * quantity,
            Currency: "BRL",
            Items:
            [
                new OrderItem(
                    ProductId: productId,
                    ProductName: productName,
                    Quantity: quantity,
                    UnitPrice: unitPrice)
            ],
            CreatedAt: DateTime.UtcNow);
    }
}