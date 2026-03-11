using Azure.Messaging.ServiceBus.Administration;

namespace EventProcessor.Api.Infrastructure;

public class ServiceBusInitializer(IConfiguration configuration, ILogger<ServiceBusInitializer> logger)
{
    private readonly string _topicName = configuration["ServiceBus:TopicName"]!;
    private readonly string _subscriptionName = configuration["ServiceBus:SubscriptionName"]!;
    private readonly string _connectionString = configuration.GetConnectionString("ServiceBus")!;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var adminClient = new ServiceBusAdministrationClient(_connectionString);

        await EnsureTopicExistsAsync(adminClient, cancellationToken);
        await EnsureSubscriptionExistsAsync(adminClient, cancellationToken);
    }

    private async Task EnsureTopicExistsAsync(ServiceBusAdministrationClient adminClient, CancellationToken cancellationToken)
    {
        if (await adminClient.TopicExistsAsync(_topicName, cancellationToken))
        {
            logger.LogInformation("Topic '{TopicName}' already exists", _topicName);
            return;
        }

        logger.LogInformation("Creating topic '{TopicName}'...", _topicName);

        var options = new CreateTopicOptions(_topicName)
        {
            DefaultMessageTimeToLive = TimeSpan.FromDays(1),
            EnableBatchedOperations = true,
            MaxSizeInMegabytes = 1024,
        };

        await adminClient.CreateTopicAsync(options, cancellationToken);
        logger.LogInformation("Topic '{TopicName}' created successfully", _topicName);
    }

    private async Task EnsureSubscriptionExistsAsync(ServiceBusAdministrationClient adminClient, CancellationToken cancellationToken)
    {
        if (await adminClient.SubscriptionExistsAsync(_topicName, _subscriptionName, cancellationToken))
        {
            logger.LogInformation("Subscription '{SubscriptionName}' on topic '{TopicName}' already exists",
                _subscriptionName, _topicName);
            return;
        }

        logger.LogInformation("Creating subscription '{SubscriptionName}' on topic '{TopicName}'...",
            _subscriptionName, _topicName);

        var options = new CreateSubscriptionOptions(_topicName, _subscriptionName)
        {
            DefaultMessageTimeToLive = TimeSpan.FromDays(1),
            LockDuration = TimeSpan.FromSeconds(30),
            MaxDeliveryCount = 5,
            EnableBatchedOperations = true,
            DeadLetteringOnMessageExpiration = true,
        };

        await adminClient.CreateSubscriptionAsync(options, cancellationToken);
        logger.LogInformation("Subscription '{SubscriptionName}' created successfully", _subscriptionName);
    }
}
