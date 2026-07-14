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
        // delay proposital para demo — dá tempo de ver a mensagem na fila no RabbitMQ Management UI
        await Task.Delay(TimeSpan.FromSeconds(5));

        var result = await _validator.ValidateAsync(context.Message);

        if (!result.IsValid)
        {
            _logger.LogWarning("Falha na validação de OrderPlacedEvent: {Errors}", result.ToString());
            return;
        }

        var processed = _paymentService.ProcessPayment(context.Message);

        await context.Publish(processed);

        _logger.LogInformation("Pagamento processado para OrderId {OrderId} com Status {Status}", processed.OrderId, processed.Status);
    }
}
