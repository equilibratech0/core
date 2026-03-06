namespace AccountBalance.Core.Tests.Shared.Entities;

using FluentAssertions;
using global::Shared.Domain.Entities;
using global::Shared.Domain.Enums;
using global::Shared.Domain.Exceptions;

public class MovementTests
{
    private static Amount CreateValidAmount(decimal total = 100m, Currency currency = Currency.USD) =>
        new(total, currency, total + 5m, total - 2m, 3m, 4m);

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        var amount = CreateValidAmount();
        var paymentMethod = new PaymentMethodDetails("pm-1", "Stripe", PaymentMethodType.CreditCard);
        var merchant = new MerchantDetails("merch-1", "TestMerchant", new ShopDetails("shop-1", "TestShop"));

        var movement = Movement.Create(
            MovementEventType.TransactionCreated,
            amount,
            "txn-001",
            "acc-001",
            "US",
            paymentMethod,
            merchant,
            "Test description");

        movement.Id.Should().NotBeNull();
        movement.EventType.Should().Be(MovementEventType.TransactionCreated);
        movement.Amount.Should().Be(amount);
        movement.TransactionId.Should().Be("txn-001");
        movement.AccountId.Should().Be("acc-001");
        movement.Country.Should().Be("US");
        movement.PaymentMethod.Should().Be(paymentMethod);
        movement.Merchant.Should().Be(merchant);
        movement.Description.Should().Be("Test description");
        movement.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullAmount_ShouldThrowDomainException()
    {
        var act = () => Movement.Create(
            MovementEventType.TransactionCreated, null!, "txn-001", null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*Amount*");
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldSucceed()
    {
        var amount = CreateValidAmount();

        var movement = Movement.Create(
            MovementEventType.PayoutCreated,
            amount,
            "txn-002",
            null,
            null,
            null,
            null,
            null);

        movement.AccountId.Should().BeNull();
        movement.Country.Should().BeNull();
        movement.PaymentMethod.Should().BeNull();
        movement.Merchant.Should().BeNull();
        movement.Description.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var amount = CreateValidAmount();

        var m1 = Movement.Create(MovementEventType.TransactionCreated, amount, "txn-1", null, null, null);
        var m2 = Movement.Create(MovementEventType.TransactionCreated, amount, "txn-2", null, null, null);

        m1.Id.Should().NotBe(m2.Id);
    }

    [Theory]
    [InlineData(MovementEventType.TransactionCreated)]
    [InlineData(MovementEventType.PayoutFinished)]
    [InlineData(MovementEventType.ClaimOpen)]
    [InlineData(MovementEventType.SettlementPublished)]
    public void Create_WithDifferentEventTypes_ShouldStoreCorrectly(MovementEventType eventType)
    {
        var amount = CreateValidAmount();

        var movement = Movement.Create(eventType, amount, "txn-001", null, null, null);

        movement.EventType.Should().Be(eventType);
    }

    [Fact]
    public void Create_WithMerchantAndShop_ShouldStoreNestedDetails()
    {
        var amount = CreateValidAmount();
        var shop = new ShopDetails("shop-1", "MyShop");
        var merchant = new MerchantDetails("merch-1", "MyMerchant", shop);

        var movement = Movement.Create(
            MovementEventType.TransactionCreated, amount, "txn-001", null, null, null, merchant);

        movement.Merchant.Should().NotBeNull();
        movement.Merchant!.MerchantId.Should().Be("merch-1");
        movement.Merchant.Shop.Should().NotBeNull();
        movement.Merchant.Shop!.ShopId.Should().Be("shop-1");
        movement.Merchant.Shop.ShopName.Should().Be("MyShop");
    }
}
