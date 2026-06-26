namespace FCG.Payments.Domain.Dto;

public sealed class OrderPlacedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public decimal Price { get; init; }
}
