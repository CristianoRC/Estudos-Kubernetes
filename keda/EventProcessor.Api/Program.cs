using EventProcessor.Api.AsyncApi;
using EventProcessor.Api.Infrastructure;
using EventProcessor.Api.Services;
using EventProcessor.Api.Workers;
using Microsoft.OpenApi;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Event Processor API",
            Version = "v1",
            Description = "REST API for publishing order domain events to Azure Service Bus. " +
                          "Messages are wrapped in a CloudEvents 1.0 envelope.",
            Contact = new OpenApiContact { Name = "Event Processor Team" },
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    options.AssemblyMarkerTypes = [typeof(OrderEventsChannels)];
    options.AsyncApi = new AsyncApiDocument
    {
        Info = new Info("Event Processor - Async API", "1.0.0")
        {
            Description = "AsyncAPI spec for order domain events published to and consumed from Azure Service Bus. " +
                          "CloudEvents 1.0 structured content mode is used as the message envelope.",
        },
        Servers =
        {
            ["azure-service-bus"] = new Server(
            $"sb://{builder.Configuration["ServiceBus:Namespace"] ?? "<namespace>.servicebus.windows.net"}",
                "amqp")
            {
                Description = "Azure Service Bus namespace",
            },
        },
    };
});



builder.Services.AddSingleton<ServiceBusInitializer>();
builder.Services.AddSingleton<EventPublisherService>();
builder.Services.AddHostedService<EventConsumerWorker>();

var app = builder.Build();


app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Event Processor API";
    options.Theme = ScalarTheme.DeepSpace;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
});


app.MapAsyncApiDocuments();
app.MapAsyncApiUi();


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