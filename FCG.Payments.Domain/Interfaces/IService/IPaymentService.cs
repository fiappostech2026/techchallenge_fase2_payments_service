using FCG.Payments.Domain.Dto;

namespace FCG.Payments.Domain.Interfaces.IService;

public interface IPaymentService
{
    PaymentProcessedEvent ProcessPayment(OrderPlacedEvent orderEvent);
}
