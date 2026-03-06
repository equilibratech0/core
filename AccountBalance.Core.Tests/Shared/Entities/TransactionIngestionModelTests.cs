namespace AccountBalance.Core.Tests.Shared.Entities;

using FluentAssertions;
using global::Shared.Domain.Entities;
using global::Shared.Domain.Enums;

public class TransactionIngestionModelTests
{
    private static ClientContext CreateValidContext() =>
        new(Guid.NewGuid(), "TestClient", new List<string> { "user-1", "user-2" });

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var context = CreateValidContext();
        var idempotencyKey = "idem-key-001";
        var eventType = MovementEventType.TransactionCreated;

        var model = new TransactionIngestionModel(context, idempotencyKey, eventType);

        model.Id.Should().NotBe(Guid.Empty);
        model.ClientId.Should().Be(context.ClientId);
        model.ClientName.Should().Be(context.ClientName);
        model.UserIds.Should().BeEquivalentTo(context.UserIds);
        model.IdempotencyKey.Should().Be(idempotencyKey);
        model.EventType.Should().Be(eventType);
        model.ReceivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNullClientContext_ShouldThrowArgumentNullException()
    {
        var act = () => new TransactionIngestionModel(null!, "key", MovementEventType.TransactionCreated);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullIdempotencyKey_ShouldThrowArgumentNullException()
    {
        var context = CreateValidContext();

        var act = () => new TransactionIngestionModel(context, null!, MovementEventType.TransactionCreated);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("idempotencyKey");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var context = CreateValidContext();

        var model1 = new TransactionIngestionModel(context, "key-1", MovementEventType.TopupCreated);
        var model2 = new TransactionIngestionModel(context, "key-2", MovementEventType.PayoutCreated);

        model1.Id.Should().NotBe(model2.Id);
    }

    [Theory]
    [InlineData(MovementEventType.TransactionCreated)]
    [InlineData(MovementEventType.PayoutFinished)]
    [InlineData(MovementEventType.ChargebackOpen)]
    [InlineData(MovementEventType.SettlementPublished)]
    public void Constructor_WithDifferentEventTypes_ShouldStoreCorrectly(MovementEventType eventType)
    {
        var context = CreateValidContext();

        var model = new TransactionIngestionModel(context, "key", eventType);

        model.EventType.Should().Be(eventType);
    }
}
