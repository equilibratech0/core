namespace AccountBalance.Core.Tests.Shared.Entities;

using FluentAssertions;
using global::Shared.Domain.Entities;
using global::Shared.Domain.Enums;

public class AmountTests
{
    [Fact]
    public void Constructor_WithAllParameters_ShouldInitializeCorrectly()
    {
        var amount = new Amount(100m, Currency.USD, 105m, 98m, 3m, 4m);

        amount.TotalAmount.Should().Be(100m);
        amount.Currency.Should().Be(Currency.USD);
        amount.GrossAmount.Should().Be(105m);
        amount.NetAmount.Should().Be(98m);
        amount.PaymentFee.Should().Be(3m);
        amount.PlatformFee.Should().Be(4m);
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_ShouldSucceed()
    {
        var amount = new Amount(50m, Currency.EUR, null, null, null, null);

        amount.TotalAmount.Should().Be(50m);
        amount.Currency.Should().Be(Currency.EUR);
        amount.GrossAmount.Should().BeNull();
        amount.NetAmount.Should().BeNull();
        amount.PaymentFee.Should().BeNull();
        amount.PlatformFee.Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldContainAllValues()
    {
        var amount = new Amount(100m, Currency.MXN, 105m, 98m, 3m, 4m);

        var result = amount.ToString();

        result.Should().Contain("100");
        result.Should().Contain("MXN");
        result.Should().Contain("105");
        result.Should().Contain("98");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a1 = new Amount(100m, Currency.USD, 105m, 98m, 3m, 4m);
        var a2 = new Amount(100m, Currency.USD, 105m, 98m, 3m, 4m);

        a1.Should().Be(a2);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var a1 = new Amount(100m, Currency.USD, 105m, 98m, 3m, 4m);
        var a2 = new Amount(200m, Currency.USD, 105m, 98m, 3m, 4m);

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Equality_DifferentCurrency_ShouldNotBeEqual()
    {
        var a1 = new Amount(100m, Currency.USD, null, null, null, null);
        var a2 = new Amount(100m, Currency.EUR, null, null, null, null);

        a1.Should().NotBe(a2);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ShouldSucceed()
    {
        var amount = new Amount(0m, Currency.GBP, null, null, null, null);

        amount.TotalAmount.Should().Be(0m);
    }
}
