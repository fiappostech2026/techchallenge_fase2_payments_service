using FCG.Payments.Worker.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.Services.AddDependencies();
builder.Services.AddMassTransitWithRabbitMQ(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseErrorHandling();
app.MapHealthChecks("/healthz");
app.Run();
