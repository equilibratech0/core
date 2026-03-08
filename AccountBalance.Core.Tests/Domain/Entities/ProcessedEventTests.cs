namespace AccountBalance.Core.Tests.Domain.Entities;

using FluentAssertions;
using AccountBalance.Core.Domain.Entities;

public class ProcessedEventTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var transactionId = Guid.NewGuid();

        var processedEvent = new ProcessedEvent(transactionId);

        processedEvent.Id.Should().NotBe(Guid.Empty);
        processedEvent.TransactionId.Should().Be(transactionId);
        processedEvent.ProcessedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var event1 = new ProcessedEvent(Guid.NewGuid());
        var event2 = new ProcessedEvent(Guid.NewGuid());

        event1.Id.Should().NotBe(event2.Id);
    }

    [Fact]
    public void Constructor_WithEmptyGuidTransactionId_ShouldSucceed()
    {
        var processedEvent = new ProcessedEvent(Guid.Empty);

        processedEvent.TransactionId.Should().Be(Guid.Empty);
    }
}
