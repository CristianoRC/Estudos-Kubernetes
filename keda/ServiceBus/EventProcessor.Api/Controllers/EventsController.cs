using EventProcessor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventProcessor.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(EventPublisherService publisher, IConfiguration configuration) : ControllerBase
{
    [HttpPost("publish")]
    [ProducesResponseType<PublishResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Publish(CancellationToken cancellationToken)
    {
        var published = await publisher.PublishOrderCreatedEventsAsync(50, cancellationToken);
        var response = new PublishResult(
            Message: $"Successfully published {published} events.",
            Count: published,
            EventType: "com.ecommerce.order.created",
            Topic: configuration["ServiceBus:TopicName"]!,
            PublishedAt: DateTimeOffset.UtcNow);

        return Ok(response);
    }
}

public record PublishResult(string Message, int Count, string EventType, string Topic, DateTimeOffset PublishedAt);