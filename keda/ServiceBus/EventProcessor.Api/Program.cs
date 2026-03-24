using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using EventProcessor.Api.Infrastructure;
using EventProcessor.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ServiceBus")
    ?? throw new InvalidOperationException("'ConnectionStrings:ServiceBus' is required.");

builder.Services.AddSingleton(new ServiceBusClient(connectionString));
builder.Services.AddSingleton(new ServiceBusAdministrationClient(connectionString));

builder.Services.AddSingleton<ServiceBusInitializer>();
builder.Services.AddHostedService<EventConsumerWorker>();

var app = builder.Build();

var initializer = app.Services.GetRequiredService<ServiceBusInitializer>();
var initLogger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    initLogger.LogInformation("Initializing Azure Service Bus resources...");
    await initializer.InitializeAsync();
    initLogger.LogInformation("Azure Service Bus resources initialized successfully");
}
catch (Exception ex)
{
    initLogger.LogError(ex, "Failed to initialize Azure Service Bus resources");
    throw;
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();
