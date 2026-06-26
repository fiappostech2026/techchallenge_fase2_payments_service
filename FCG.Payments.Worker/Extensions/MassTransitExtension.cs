using FCG.Payments.Domain.Dto;
using FCG.Payments.Worker.Consumers;
using MassTransit;

namespace FCG.Payments.Worker.Extensions;

public static class MassTransitExtension
{
    public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        var host = configuration["RabbitMQ:Host"] ?? "localhost";
        var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<OrderPlacedEventConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, virtualHost, rabbitHost =>
                {
                    rabbitHost.Username(username);
                    rabbitHost.Password(password);
                });

                cfg.Message<OrderPlacedEvent>(msgCfg => msgCfg.SetEntityName("order-placed-event"));
                cfg.Message<PaymentProcessedEvent>(msgCfg => msgCfg.SetEntityName("payment-processed-event"));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
