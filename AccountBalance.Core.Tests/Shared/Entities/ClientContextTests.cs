namespace AccountBalance.Core.Tests.Shared.Entities;

using FluentAssertions;
using global::Shared.Domain.Entities;

public class ClientContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var clientId = Guid.NewGuid();
        var clientName = "TestClient";
        var userIds = new List<string> { "user-1", "user-2" };

        var context = new ClientContext(clientId, clientName, userIds);

        context.ClientId.Should().Be(clientId);
        context.ClientName.Should().Be(clientName);
        context.UserIds.Should().BeEquivalentTo(userIds);
    }

    [Fact]
    public void Constructor_WithNullClientName_ShouldThrowArgumentNullException()
    {
        var act = () => new ClientContext(Guid.NewGuid(), null!, new List<string>());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clientName");
    }

    [Fact]
    public void Constructor_WithNullUserIds_ShouldDefaultToEmptyList()
    {
        var context = new ClientContext(Guid.NewGuid(), "Client", null!);

        context.UserIds.Should().NotBeNull();
        context.UserIds.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldSucceed()
    {
        var context = new ClientContext(Guid.Empty, "Client", new List<string>());

        context.ClientId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_WithEmptyUserIds_ShouldStoreEmptyList()
    {
        var context = new ClientContext(Guid.NewGuid(), "Client", new List<string>());

        context.UserIds.Should().BeEmpty();
    }
}
