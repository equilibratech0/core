using AccountBalance.Core.Infrastructure.DependencyInjection;
using AccountBalance.Core.Worker.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.AddCoreInfrastructure(builder.Configuration);
builder.Services.AddCoreApplication();
builder.Services.AddHostedService<TransactionReceivedConsumer>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapGet("/status", (IConfiguration config) =>
{
    var asbConn = config["AzureServiceBus:ConnectionString"];
    var mongoConn = config["MongoDb:ConnectionString"];
    return Results.Ok(new
    {
        status = "running",
        time = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        config = new
        {
            azureServiceBus_connectionString = string.IsNullOrWhiteSpace(asbConn) ? "NOT SET" : $"configured ({asbConn.Length} chars)",
            azureServiceBus_topicName = config["AzureServiceBus:TopicName"] ?? "NOT SET",
            azureServiceBus_subscriptionName = config["AzureServiceBus:SubscriptionName"] ?? "NOT SET",
            mongoDb_connectionString = string.IsNullOrWhiteSpace(mongoConn) ? "NOT SET" : "configured",
            mongoDb_databaseName = config["MongoDb:DatabaseName"] ?? "NOT SET"
        }
    });
});

app.Run();
