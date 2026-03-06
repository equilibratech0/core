namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;

public class ClientAccountMappingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var clientId = Guid.NewGuid();
        var clientName = "TestClient";
        var accountId = "ACC-001";
        var userIds = new List<string> { "user-1", "user-2" };

        var mapping = new ClientAccountMapping(clientId, clientName, accountId, userIds);

        mapping.Id.Should().NotBe(Guid.Empty);
        mapping.ClientId.Should().Be(clientId);
        mapping.ClientName.Should().Be(clientName);
        mapping.AccountId.Should().Be(accountId);
        mapping.UserIds.Should().BeEquivalentTo(userIds);
        mapping.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        mapping.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNullClientName_ShouldThrowArgumentNullException()
    {
        var act = () => new ClientAccountMapping(Guid.NewGuid(), null!, "ACC-001", new List<string>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clientName");
    }

    [Fact]
    public void Constructor_WithNullAccountId_ShouldThrowArgumentNullException()
    {
        var act = () => new ClientAccountMapping(Guid.NewGuid(), "Client", null!, new List<string>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("accountId");
    }

    [Fact]
    public void Constructor_WithNullUserIds_ShouldDefaultToEmptyList()
    {
        var mapping = new ClientAccountMapping(Guid.NewGuid(), "Client", "ACC-001", null!);

        mapping.UserIds.Should().NotBeNull();
        mapping.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateAccountIdAndUserIds()
    {
        var mapping = new ClientAccountMapping(Guid.NewGuid(), "Client", "ACC-001", new List<string> { "user-1" });
        var originalUpdatedAt = mapping.UpdatedAt;

        var newUserIds = new List<string> { "user-2", "user-3" };
        mapping.Update("ACC-002", newUserIds);

        mapping.AccountId.Should().Be("ACC-002");
        mapping.UserIds.Should().BeEquivalentTo(newUserIds);
        mapping.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void Update_WithNullAccountId_ShouldThrowArgumentNullException()
    {
        var mapping = new ClientAccountMapping(Guid.NewGuid(), "Client", "ACC-001", new List<string>());

        var act = () => mapping.Update(null!, new List<string>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("accountId");
    }

    [Fact]
    public void Update_WithNullUserIds_ShouldDefaultToEmptyList()
    {
        var mapping = new ClientAccountMapping(Guid.NewGuid(), "Client", "ACC-001", new List<string> { "user-1" });

        mapping.Update("ACC-002", null!);

        mapping.UserIds.Should().NotBeNull();
        mapping.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var mapping1 = new ClientAccountMapping(Guid.NewGuid(), "A", "ACC-1", new List<string>());
        var mapping2 = new ClientAccountMapping(Guid.NewGuid(), "B", "ACC-2", new List<string>());

        mapping1.Id.Should().NotBe(mapping2.Id);
    }
}
