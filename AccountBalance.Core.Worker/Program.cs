using AccountBalance.Core.Infrastructure.DependencyInjection;
using AccountBalance.Core.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddCoreApplication();
builder.Services.AddHostedService<TransactionReceivedConsumer>();

var host = builder.Build();
host.Run();
