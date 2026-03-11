using EventProcessor.Api.Infrastructure;
using EventProcessor.Api.Services;
using EventProcessor.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ServiceBusInitializer>();
builder.Services.AddSingleton<EventPublisherService>();
builder.Services.AddHostedService<EventConsumerWorker>();

var app = builder.Build();
app.MapOpenApi();

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
    initLogger.LogError(ex, "Failed to initialize Azure Service Bus resources. The application will still start, but messaging may not work");
    throw;
}

app.MapControllers();
app.Run();