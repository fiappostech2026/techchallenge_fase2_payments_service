using FCG.Payments.Domain.Interfaces.IService;
using FCG.Payments.Domain.Services;
using FCG.Payments.Domain.Validators;
using FluentValidation;

namespace FCG.Payments.Worker.Extensions;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependencies(this IServiceCollection services)
    {
        services.AddTransient<IPaymentService>(_ => new PaymentService());
        services.AddValidatorsFromAssemblyContaining<OrderPlacedEventValidator>();

        return services;
    }
}
