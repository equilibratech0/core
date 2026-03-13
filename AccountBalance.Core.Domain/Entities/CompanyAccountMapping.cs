namespace AccountBalance.Core.Domain.Entities;

public class CompanyAccountMapping
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid AccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CompanyAccountMapping() { }

    public CompanyAccountMapping(Guid companyId, Guid accountId)
    {
        Id = Guid.NewGuid();
        CompanyId = companyId;
        AccountId = accountId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(Guid accountId)
    {
        AccountId = accountId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
