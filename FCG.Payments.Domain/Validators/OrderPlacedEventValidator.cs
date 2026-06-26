using FCG.Payments.Domain.Dto;
using FluentValidation;

namespace FCG.Payments.Domain.Validators;

public sealed class OrderPlacedEventValidator : AbstractValidator<OrderPlacedEvent>
{
    public OrderPlacedEventValidator()
    {
        RuleFor(evt => evt.OrderId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.GameId)
            .NotEqual(Guid.Empty);

        RuleFor(evt => evt.Price)
            .GreaterThan(0);
    }
}
