using AccountBalance.Core.Infrastructure.DependencyInjection;
using AccountBalance.Core.Worker.Consumers;

Console.WriteLine("=== AccountBalance.Core.Worker starting ===");
Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}");
Console.WriteLine($"Time: {DateTime.UtcNow:O}");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.AddConsole();
    builder.Logging.AddAzureWebAppDiagnostics();

    var asbConnStr = builder.Configuration["AzureServiceBus:ConnectionString"];
    Console.WriteLine($"AzureServiceBus:ConnectionString configured: {!string.IsNullOrWhiteSpace(asbConnStr)}");
    Console.WriteLine($"AzureServiceBus:TopicName: {builder.Configuration["AzureServiceBus:TopicName"]}");
    Console.WriteLine($"MongoDb:ConnectionString configured: {!string.IsNullOrWhiteSpace(builder.Configuration["MongoDb:ConnectionString"])}");

    builder.Services.AddCoreInfrastructure(builder.Configuration);
    builder.Services.AddCoreApplication();
    builder.Services.AddHostedService<TransactionReceivedConsumer>();

    var app = builder.Build();

    app.MapGet("/health", () => Results.Ok("healthy"));

    Console.WriteLine("=== App built successfully, starting... ===");
    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"=== FATAL: Application failed to start ===");
    Console.Error.WriteLine(ex.ToString());
    throw;
}
