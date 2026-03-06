namespace AccountBalance.Core.Domain.Aggregates;

using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.ValueObjects;

public class AccountBalanceEntry : AggregateRoot<AccountBalanceId>
{
    public Guid ClientId { get; private set; }
    public string AccountId { get; private set; } = null!;
    public Currency Currency { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal TotalPayins { get; private set; }
    public decimal TotalPayouts { get; private set; }
    public DateTimeOffset? LastMovementAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    protected AccountBalanceEntry() { }

    private AccountBalanceEntry(AccountBalanceId id, Guid clientId, string accountId, Currency currency)
    {
        Id = id;
        ClientId = clientId;
        AccountId = accountId;
        Currency = currency;
        AvailableBalance = 0m;
        TotalPayins = 0m;
        TotalPayouts = 0m;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static AccountBalanceEntry Create(Guid clientId, string accountId, Currency currency)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new DomainException("AccountId cannot be null or empty.");

        return new AccountBalanceEntry(AccountBalanceId.New(), clientId, accountId, currency);
    }

    public void AddBalance(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Credit amount must be positive.");

        AvailableBalance += amount;
        TotalPayins += amount;
        Touch();
    }

    public void SubtractBalance(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Debit amount must be positive.");

        AvailableBalance -= amount;
        TotalPayouts += amount;
        Touch();
    }

    private void Touch()
    {
        LastMovementAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
