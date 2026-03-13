namespace AccountBalance.Core.Domain.Aggregates;

using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.ValueObjects;

public class AccountBalanceEntry : AggregateRoot<AccountBalanceId>
{
    public Guid CompanyId { get; private set; }
    public Guid AccountId { get; private set; }
    public Currency Currency { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal TotalPayins { get; private set; }
    public decimal TotalPayouts { get; private set; }
    public DateTimeOffset? LastMovementAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    protected AccountBalanceEntry() { }

    private AccountBalanceEntry(AccountBalanceId id, Guid companyId, Guid accountId, Currency currency)
    {
        Id = id;
        CompanyId = companyId;
        AccountId = accountId;
        Currency = currency;
        AvailableBalance = 0m;
        TotalPayins = 0m;
        TotalPayouts = 0m;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static AccountBalanceEntry Create(Guid companyId, Guid accountId, Currency currency)
    {
        if (accountId == Guid.Empty)
            throw new DomainException("AccountId cannot be empty.");

        return new AccountBalanceEntry(AccountBalanceId.New(), companyId, accountId, currency);
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
