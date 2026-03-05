namespace AccountBalance.Core.Domain.Repositories;

using Shared.Domain.Enums;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.ValueObjects;

public interface IAccountBalanceRepository
{
    Task<AccountBalanceEntry?> GetByAccountAndCurrencyAsync(Guid clientId, string accountId, Currency currency, CancellationToken cancellationToken = default);
    Task AddAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default);
}
