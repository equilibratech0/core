namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;

public class CompanyAccountMappingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var companyId = Guid.NewGuid();
        var accountId = "ACC-001";

        var mapping = new CompanyAccountMapping(companyId, accountId);

        mapping.Id.Should().NotBe(Guid.Empty);
        mapping.CompanyId.Should().Be(companyId);
        mapping.AccountId.Should().Be(accountId);
        mapping.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        mapping.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNullAccountId_ShouldThrowArgumentNullException()
    {
        var act = () => new CompanyAccountMapping(Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("accountId");
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateAccountId()
    {
        var mapping = new CompanyAccountMapping(Guid.NewGuid(), "ACC-001");
        var originalUpdatedAt = mapping.UpdatedAt;

        mapping.Update("ACC-002");

        mapping.AccountId.Should().Be("ACC-002");
        mapping.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_WithNullAccountId_ShouldThrowArgumentNullException()
    {
        var mapping = new CompanyAccountMapping(Guid.NewGuid(), "ACC-001");

        var act = () => mapping.Update(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("accountId");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var mapping1 = new CompanyAccountMapping(Guid.NewGuid(), "ACC-1");
        var mapping2 = new CompanyAccountMapping(Guid.NewGuid(), "ACC-2");

        mapping1.Id.Should().NotBe(mapping2.Id);
    }
}
