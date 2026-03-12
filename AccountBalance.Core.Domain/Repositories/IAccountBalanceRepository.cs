namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Aggregates;
using Shared.Domain.Enums;

public interface IAccountBalanceRepository
{
    Task<AccountBalanceEntry?> GetByAccountAsync(Guid companyId, string accountId, Currency currency, CancellationToken cancellationToken = default);
    Task UpsertAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default);
}
