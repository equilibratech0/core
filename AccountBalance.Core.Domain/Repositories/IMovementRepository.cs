namespace AccountBalance.Core.Domain.Repositories;

using Shared.Domain.Entities;

public interface IMovementRepository
{
    Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(Movement movement, CancellationToken cancellationToken = default);
}
