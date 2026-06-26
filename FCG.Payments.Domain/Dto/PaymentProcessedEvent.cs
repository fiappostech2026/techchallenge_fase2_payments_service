using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Dto;

public sealed class PaymentProcessedEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public Guid GameId { get; init; }
    public PaymentStatus Status { get; init; }
}
