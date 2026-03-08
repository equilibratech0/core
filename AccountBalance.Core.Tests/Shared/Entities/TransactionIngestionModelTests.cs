namespace AccountBalance.Core.Tests.Shared.Entities;

using FluentAssertions;
using global::Shared.Domain.Entities;

public class TransactionIngestionModelTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var idempotencyKey = "company-123:idem-key-001";

        var model = new TransactionIngestionModel(idempotencyKey);

        model.Id.Should().NotBe(Guid.Empty);
        model.IdempotencyKey.Should().Be(idempotencyKey);
        model.ReceivedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_WithNullIdempotencyKey_ShouldThrowArgumentNullException()
    {
        var act = () => new TransactionIngestionModel(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("idempotencyKey");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        var model1 = new TransactionIngestionModel("key-1");
        var model2 = new TransactionIngestionModel("key-2");

        model1.Id.Should().NotBe(model2.Id);
    }
}
