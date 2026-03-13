namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;

public class CompanyAccountMappingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var companyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var mapping = new CompanyAccountMapping(companyId, accountId);

        mapping.Id.Should().NotBe(Guid.Empty);
        mapping.CompanyId.Should().Be(companyId);
        mapping.AccountId.Should().Be(accountId);
        mapping.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        mapping.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateAccountId()
    {
        var mapping = new CompanyAccountMapping(Guid.NewGuid(), Guid.NewGuid());
        var originalUpdatedAt = mapping.UpdatedAt;
        var newAccountId = Guid.NewGuid();

        mapping.Update(newAccountId);

        mapping.AccountId.Should().Be(newAccountId);
        mapping.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var mapping1 = new CompanyAccountMapping(Guid.NewGuid(), Guid.NewGuid());
        var mapping2 = new CompanyAccountMapping(Guid.NewGuid(), Guid.NewGuid());

        mapping1.Id.Should().NotBe(mapping2.Id);
    }
}
