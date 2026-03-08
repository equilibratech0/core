namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Entities;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
