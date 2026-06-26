using FCG.Payments.Domain.Dto;
using FCG.Payments.Domain.Interfaces.IService;
using FluentValidation;
using MassTransit;

namespace FCG.Payments.Worker.Consumers;

public sealed class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly IValidator<OrderPlacedEvent> _validator;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<OrderPlacedEventConsumer> _logger;

    public OrderPlacedEventConsumer(
        IValidator<OrderPlacedEvent> validator,
        IPaymentService paymentService,
        ILogger<OrderPlacedEventConsumer> logger)
    {
        _validator = validator;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("OrderPlacedEvent validation failed: {Errors}", result.ToString());
            return;
        }

        var processed = _paymentService.ProcessPayment(context.Message);

        await context.Publish(processed);

        _logger.LogInformation("Payment processed for OrderId {OrderId} with Status {Status}", processed.OrderId, processed.Status);
    }
}
