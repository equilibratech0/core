namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;

public class ProcessedEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var idempotencyKey = "txn-abc-123";
        var sourceTransactionId = Guid.NewGuid();

        var processedEvent = new ProcessedEvent(idempotencyKey, sourceTransactionId);

        processedEvent.Id.Should().NotBe(Guid.Empty);
        processedEvent.IdempotencyKey.Should().Be(idempotencyKey);
        processedEvent.SourceTransactionId.Should().Be(sourceTransactionId);
        processedEvent.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNullIdempotencyKey_ShouldThrowArgumentNullException()
    {
        var act = () => new ProcessedEvent(null!, Guid.NewGuid());

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("idempotencyKey");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var event1 = new ProcessedEvent("key-1", Guid.NewGuid());
        var event2 = new ProcessedEvent("key-2", Guid.NewGuid());

        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_WithEmptyGuidTransactionId_ShouldSucceed()
    {
        var processedEvent = new ProcessedEvent("key", Guid.Empty);

        processedEvent.SourceTransactionId.Should().Be(Guid.Empty);
    }
}
