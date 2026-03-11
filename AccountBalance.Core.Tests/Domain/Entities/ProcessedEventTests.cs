namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;
using global::Shared.Domain.Enums;

public class ProcessedEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var transactionId = Guid.NewGuid();
        var eventType = MovementEventType.TransactionApproved;

        var processedEvent = new ProcessedEvent(transactionId, eventType);

        processedEvent.Id.Should().NotBe(Guid.Empty);
        processedEvent.TransactionId.Should().Be(transactionId);
        processedEvent.EventType.Should().Be(eventType);
        processedEvent.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var event1 = new ProcessedEvent(Guid.NewGuid(), MovementEventType.TransactionApproved);
        var event2 = new ProcessedEvent(Guid.NewGuid(), MovementEventType.PayoutFinished);

        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_WithEmptyGuidTransactionId_ShouldSucceed()
    {
        var processedEvent = new ProcessedEvent(Guid.Empty, MovementEventType.TransactionApproved);

        processedEvent.TransactionId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Constructor_SameTransactionDifferentEventType_ShouldBothSucceed()
    {
        var transactionId = Guid.NewGuid();

        var event1 = new ProcessedEvent(transactionId, MovementEventType.TransactionApproved);
        var event2 = new ProcessedEvent(transactionId, MovementEventType.ChargebackClose);

        event1.TransactionId.Should().Be(event2.TransactionId);
        event1.EventType.Should().NotBe(event2.EventType);
    }
}
