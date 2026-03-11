using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using EventProcessor.Api.Models;

namespace EventProcessor.Api.Workers;

public class EventConsumerWorker(IConfiguration configuration, ILogger<EventConsumerWorker> logger) : BackgroundService
{
    private static readonly JsonEventFormatter CloudEventFormatter = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _connectionString = configuration.GetConnectionString("ServiceBus")!;
    private readonly string _topicName = configuration["ServiceBus:TopicName"]!;
    private readonly string _subscriptionName = configuration["ServiceBus:SubscriptionName"]!;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EventConsumerWorker starting. Listening on topic '{Topic}', subscription '{Subscription}'", _topicName, _subscriptionName);

        await using var client = new ServiceBusClient(_connectionString);

        var processorOptions = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 5,
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
        };

        await using var processor = client.CreateProcessor(_topicName, _subscriptionName, processorOptions);

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);

        logger.LogInformation("EventConsumerWorker is running and waiting for messages");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("EventConsumerWorker stopping...");
        }
        finally
        {
            await processor.StopProcessingAsync(stoppingToken);
            logger.LogInformation("EventConsumerWorker stopped");
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;

        try
        {
            logger.LogDebug("Received message '{MessageId}' with subject '{Subject}'",
                message.MessageId, message.Subject);

            var cloudEvent = ParseCloudEvent(message);

            if (cloudEvent is null)
            {
                logger.LogWarning("Message '{MessageId}' could not be parsed as a CloudEvent. Dead-lettering", message.MessageId);

                await args.DeadLetterMessageAsync(message, "InvalidCloudEvent",
                    "Could not parse message as a CloudEvent.", args.CancellationToken);
                return;
            }

            await DispatchEventAsync(cloudEvent, args.CancellationToken);

            await args.CompleteMessageAsync(message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing message '{MessageId}'. Message will be abandoned", message.MessageId);
            await args.AbandonMessageAsync(message, cancellationToken: args.CancellationToken);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}",
            args.ErrorSource, args.EntityPath);

        return Task.CompletedTask;
    }

    private CloudEvent? ParseCloudEvent(ServiceBusReceivedMessage message)
    {
        try
        {
            var bodyMemory = message.Body.ToMemory();
            return CloudEventFormatter.DecodeStructuredModeMessage(bodyMemory, null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize CloudEvent from message '{MessageId}'", message.MessageId);
            return null;
        }
    }

    private async Task DispatchEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        switch (cloudEvent.Type)
        {
            case "com.ecommerce.order.created":
                await HandleOrderCreatedAsync(cloudEvent, cancellationToken);
                break;

            default:
                logger.LogWarning("No handler registered for event type '{EventType}'. Skipping", cloudEvent.Type);
                break;
        }
    }

    private Task HandleOrderCreatedAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var order = DeserializeData<OrderCreatedEvent>(cloudEvent);

        if (order is null)
        {
            logger.LogWarning("CloudEvent '{EventId}' of type 'order.created' has null or invalid data", cloudEvent.Id);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "[ORDER CREATED] EventId={EventId} | OrderId={OrderId} | Customer={CustomerName} ({CustomerId}) | " +
            "Items={ItemCount} | Total={TotalAmount:C} {Currency} | CreatedAt={CreatedAt:O}",
            cloudEvent.Id,
            order.OrderId,
            order.CustomerName,
            order.CustomerId,
            order.Items.Count,
            order.TotalAmount,
            order.Currency,
            order.CreatedAt);

        return Task.CompletedTask;
    }

    private static T? DeserializeData<T>(CloudEvent cloudEvent)
    {
        if (cloudEvent.Data is T typedData)
            return typedData;

        if (cloudEvent.Data is JsonElement jsonElement)
            return jsonElement.Deserialize<T>(JsonOptions);

        return default;
    }
}
