namespace AccountBalance.Core.Worker.Consumers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Domain.Events;
using Shared.Infrastructure.Messaging.AzureServiceBus;
using AccountBalance.Core.Application.Commands;
using AccountBalance.Core.Application.Interfaces;

public class TransactionReceivedConsumer : AzureServiceBusConsumer<TransactionReceivedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionReceivedConsumer> _consumerLogger;

    public TransactionReceivedConsumer(
        IOptions<AzureServiceBusOptions> options,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
        : base(options, loggerFactory.CreateLogger<AzureServiceBusConsumer<TransactionReceivedEvent>>())
    {
        _serviceProvider = serviceProvider;
        _consumerLogger = loggerFactory.CreateLogger<TransactionReceivedConsumer>();
    }

    protected override async Task ProcessMessageAsync(TransactionReceivedEvent @event, CancellationToken cancellationToken)
    {
        _consumerLogger.LogInformation(
            "Received TransactionReceivedEvent: TransactionId={TransactionId}, CompanyId={CompanyId}, EventType={EventType}",
            @event.TransactionId, @event.CompanyId, @event.EventType);

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IProcessTransactionHandler>();

        var command = new ProcessTransactionCommand(
            @event.TransactionId,
            @event.CompanyId,
            @event.EventType,
            @event.RawPayload);

        await handler.HandleAsync(command, cancellationToken);

        _consumerLogger.LogInformation(
            "Successfully processed TransactionId={TransactionId}", @event.TransactionId);
    }
}
