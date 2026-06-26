using Serilog;
using Serilog.Formatting.Compact;

namespace FCG.Payments.Worker.Extensions;

public static class SerilogExtension
{
    public static IHostBuilder AddSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File("logs/payments-.log", rollingInterval: RollingInterval.Day);
        });
    }
}
