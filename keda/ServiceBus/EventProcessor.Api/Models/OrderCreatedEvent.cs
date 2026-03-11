namespace EventProcessor.Api.Models;

public record OrderCreatedEvent(
    string OrderId,
    string CustomerId,
    string CustomerName,
    decimal TotalAmount,
    string Currency,
    List<OrderItem> Items,
    DateTime CreatedAt);

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);