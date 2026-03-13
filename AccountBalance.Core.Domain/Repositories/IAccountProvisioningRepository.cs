namespace AccountBalance.Core.Domain.Repositories;

using Shared.Domain.Entities;

public interface IAccountProvisioningRepository
{
    Task<Account?> GetByReferenceAsync(Guid companyId, string accountReference, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}
