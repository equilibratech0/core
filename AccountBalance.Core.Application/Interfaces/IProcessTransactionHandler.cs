namespace AccountBalance.Core.Application.Interfaces;

using AccountBalance.Core.Application.Commands;

public interface IProcessTransactionHandler
{
    Task HandleAsync(ProcessTransactionCommand command, CancellationToken cancellationToken = default);
}
