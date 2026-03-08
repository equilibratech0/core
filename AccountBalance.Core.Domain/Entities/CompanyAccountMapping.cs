namespace AccountBalance.Core.Domain.Entities;

public class CompanyAccountMapping
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string AccountId { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CompanyAccountMapping() { }

    public CompanyAccountMapping(Guid companyId, string accountId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string accountId)
    {
        AccountId = accountId ?? throw new ArgumentNullException(nameof(accountId));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
