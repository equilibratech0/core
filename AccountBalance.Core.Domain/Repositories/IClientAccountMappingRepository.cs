namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Entities;

public interface IClientAccountMappingRepository
{
    Task<ClientAccountMapping?> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task AddAsync(ClientAccountMapping mapping, CancellationToken cancellationToken = default);
    Task UpdateAsync(ClientAccountMapping mapping, CancellationToken cancellationToken = default);
}
