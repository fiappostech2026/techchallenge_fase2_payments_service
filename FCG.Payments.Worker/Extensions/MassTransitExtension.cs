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
        // Vazio ou só espaços também falha: senão o MassTransit tenta conectar como "guest".
        var username = configuration["RabbitMQ:Username"];
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("RabbitMQ:Username não configurado (variável de ambiente RabbitMQ__Username).");
        var password = configuration["RabbitMQ:Password"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("RabbitMQ:Password não configurado (variável de ambiente RabbitMQ__Password).");
        var port = configuration.GetValue<int?>("RabbitMQ:Port");
        var useSsl = configuration.GetValue<bool>("RabbitMQ:UseSsl");

        services.AddMassTransit(bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payments", false));

            bus.AddConsumer<OrderPlacedEventConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(BuildHostUri(host, port, virtualHost, useSsl), rabbitHost =>
                {
                    rabbitHost.Username(username);
                    rabbitHost.Password(password);

                    if (useSsl)
                        rabbitHost.UseSsl(s => s.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                });

                cfg.UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true);

                cfg.Message<OrderPlacedEvent>(msgCfg => msgCfg.SetEntityName("order-placed-event"));
                cfg.Message<PaymentProcessedEvent>(msgCfg => msgCfg.SetEntityName("payment-processed-event"));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    // rabbitmqs:// (TLS) exigido pelo CloudAMQP; rabbitmq:// (sem TLS) segue funcionando local
    // com o RabbitMQ do docker-compose/k8s, sem porta/UseSsl configurados (comportamento antigo).
    private static Uri BuildHostUri(string host, int? port, string virtualHost, bool useSsl)
    {
        var scheme = useSsl ? "rabbitmqs" : "rabbitmq";
        var portSegment = port.HasValue ? $":{port}" : string.Empty;
        var vhostSegment = virtualHost == "/" ? string.Empty : $"/{Uri.EscapeDataString(virtualHost)}";

        return new Uri($"{scheme}://{host}{portSegment}{vhostSegment}");
    }
}
