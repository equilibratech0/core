namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Aggregates;

public interface IAccountBalanceRepository
{
    Task<AccountBalanceEntry?> GetByAccountAsync(Guid companyId, string accountId, CancellationToken cancellationToken = default);
    Task UpsertAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default);
}
