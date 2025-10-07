namespace BuildingBlocks.Contracts;
public record OrderCreatedEvent(Guid EventId, int OrderId, int CustomerId, decimal Total, DateTime CreatedAtUtc);
