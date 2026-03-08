namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Entities;
using Shared.Domain.Enums;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid transactionId, MovementEventType eventType, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default);
}
