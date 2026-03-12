using AccountBalance.Core.Infrastructure.DependencyInjection;
using AccountBalance.Core.Worker.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddCoreApplication();
builder.Services.AddHostedService<TransactionReceivedConsumer>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));

app.Run();
