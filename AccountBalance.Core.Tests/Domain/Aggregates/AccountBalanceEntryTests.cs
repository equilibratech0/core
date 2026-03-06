namespace AccountBalance.Core.Tests.Domain.Aggregates;

using FluentAssertions;
using global::Shared.Domain.Enums;
using global::Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.Aggregates;

public class AccountBalanceEntryTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        var clientId = Guid.NewGuid();
        var accountId = "ACC-001";
        var currency = Currency.USD;

        var entry = AccountBalanceEntry.Create(clientId, accountId, currency);

        entry.ClientId.Should().Be(clientId);
        entry.AccountId.Should().Be(accountId);
        entry.Currency.Should().Be(currency);
        entry.AvailableBalance.Should().Be(0m);
        entry.TotalPayins.Should().Be(0m);
        entry.TotalPayouts.Should().Be(0m);
        entry.LastMovementAt.Should().BeNull();
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        entry.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidAccountId_ShouldThrowDomainException(string? accountId)
    {
        var act = () => AccountBalanceEntry.Create(Guid.NewGuid(), accountId!, Currency.EUR);

        act.Should().Throw<DomainException>()
            .WithMessage("*AccountId*");
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var entry1 = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-1", Currency.USD);
        var entry2 = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-2", Currency.USD);

        entry1.Id.Should().NotBe(entry2.Id);
    }

    [Fact]
    public void AddBalance_WithPositiveAmount_ShouldIncreaseBalanceAndPayin()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);

        entry.AddBalance(100.50m);

        entry.AvailableBalance.Should().Be(100.50m);
        entry.TotalPayins.Should().Be(100.50m);
        entry.TotalPayouts.Should().Be(0m);
        entry.LastMovementAt.Should().NotBeNull();
        entry.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ApplyCredit_MultipleTimes_ShouldAccumulate()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.MXN);

        entry.AddBalance(100m);
        entry.AddBalance(250.75m);
        entry.AddBalance(49.25m);

        entry.AvailableBalance.Should().Be(400m);
        entry.TotalPayins.Should().Be(400m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void ApplyCredit_WithNonPositiveAmount_ShouldThrowDomainException(decimal amount)
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);

        var act = () => entry.AddBalance(amount);

        act.Should().Throw<DomainException>()
            .WithMessage("*Credit amount must be positive*");
    }

    [Fact]
    public void SubtractBalance_WithPositiveAmount_ShouldDecreaseBalanceAndIncreasePayout()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.EUR);
        entry.AddBalance(500m);

        entry.SubtractBalance(200m);

        entry.AvailableBalance.Should().Be(300m);
        entry.TotalPayouts.Should().Be(200m);
        entry.TotalPayins.Should().Be(500m);
    }

    [Fact]
    public void ApplyDebit_AllowsNegativeBalance()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);

        entry.SubtractBalance(100m);

        entry.AvailableBalance.Should().Be(-100m);
        entry.TotalPayouts.Should().Be(100m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50.25)]
    public void ApplyDebit_WithNonPositiveAmount_ShouldThrowDomainException(decimal amount)
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);

        var act = () => entry.SubtractBalance(amount);

        act.Should().Throw<DomainException>()
            .WithMessage("*Debit amount must be positive*");
    }

    [Fact]
    public void ApplyCredit_ThenDebit_ShouldReflectNetBalance()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.BRL);

        entry.AddBalance(1000m);
        entry.SubtractBalance(350m);
        entry.AddBalance(200m);
        entry.SubtractBalance(100m);

        entry.AvailableBalance.Should().Be(750m);
        entry.TotalPayins.Should().Be(1200m);
        entry.TotalPayouts.Should().Be(450m);
    }

    [Fact]
    public void AddBalance_ShouldUpdateLastMovementAt()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);
        entry.LastMovementAt.Should().BeNull();

        entry.AddBalance(10m);

        entry.LastMovementAt.Should().NotBeNull();
        entry.LastMovementAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SubtractBalance_ShouldUpdateLastMovementAt()
    {
        var entry = AccountBalanceEntry.Create(Guid.NewGuid(), "ACC-001", Currency.USD);
        entry.LastMovementAt.Should().BeNull();

        entry.SubtractBalance(10m);

        entry.LastMovementAt.Should().NotBeNull();
        entry.LastMovementAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
