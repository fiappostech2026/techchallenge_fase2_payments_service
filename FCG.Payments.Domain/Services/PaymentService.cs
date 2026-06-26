using FCG.Payments.Domain.Dto;
using FCG.Payments.Domain.Enums;
using FCG.Payments.Domain.Interfaces.IService;

namespace FCG.Payments.Domain.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly Func<bool> _approvalDecision;

    public PaymentService(Func<bool>? approvalDecision = null)
    {
        _approvalDecision = approvalDecision ?? (() => Random.Shared.Next(0, 2) == 0);
    }

    public PaymentProcessedEvent ProcessPayment(OrderPlacedEvent orderEvent)
    {
        ArgumentNullException.ThrowIfNull(orderEvent);

        var status = _approvalDecision() ? PaymentStatus.Approved : PaymentStatus.Rejected;

        return new PaymentProcessedEvent
        {
            OrderId = orderEvent.OrderId,
            UserId = orderEvent.UserId,
            GameId = orderEvent.GameId,
            Status = status
        };
    }
}
