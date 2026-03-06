namespace AccountBalance.Core.Tests.Domain.Services;

using FluentAssertions;
using global::Shared.Domain.Enums;
using global::Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.Enums;
using AccountBalance.Core.Domain.Services;

public class MovementClassifierTests
{
    [Theory]
    [InlineData(MovementEventType.TransactionCreated)]
    [InlineData(MovementEventType.TransactionApproved)]
    [InlineData(MovementEventType.TopupCreated)]
    [InlineData(MovementEventType.AdjustmentTopupCreated)]
    [InlineData(MovementEventType.AdjustmentRebateFeeCreated)]
    [InlineData(MovementEventType.RollingReserveReleased)]
    [InlineData(MovementEventType.ClaimClose)]
    [InlineData(MovementEventType.ClaimCloseModified)]
    [InlineData(MovementEventType.ChargebackClose)]
    [InlineData(MovementEventType.ChargebackCloseModified)]
    [InlineData(MovementEventType.PaymentOrderPaid)]
    [InlineData(MovementEventType.PaymentOrderCancelled)]
    [InlineData(MovementEventType.WithdrawalCancelled)]
    [InlineData(MovementEventType.WithdrawalReturned)]
    [InlineData(MovementEventType.PayoutError)]
    [InlineData(MovementEventType.SettlementUnpublished)]
    [InlineData(MovementEventType.PartialPayment)]
    public void Classify_PayInEventTypes_ShouldReturnPayIn(MovementEventType eventType)
    {
        var result = MovementClassifier.Classify(eventType);

        result.Should().Be(MovementDirection.PayIn);
    }

    [Theory]
    [InlineData(MovementEventType.PayoutCreated)]
    [InlineData(MovementEventType.PayoutFinished)]
    [InlineData(MovementEventType.PayoutFinishedRefund)]
    [InlineData(MovementEventType.ClaimOpen)]
    [InlineData(MovementEventType.ClaimRefund)]
    [InlineData(MovementEventType.ClaimReopen)]
    [InlineData(MovementEventType.ChargebackOpen)]
    [InlineData(MovementEventType.ChargebackReopen)]
    [InlineData(MovementEventType.ChargebackRefund)]
    [InlineData(MovementEventType.SettlementPublished)]
    [InlineData(MovementEventType.AccountSettlement)]
    [InlineData(MovementEventType.AdjustmentCreated)]
    [InlineData(MovementEventType.AdjustmentRollingReserveCreated)]
    [InlineData(MovementEventType.AdjustmentBalanceFeeCreated)]
    [InlineData(MovementEventType.WithdrawalApproved)]
    [InlineData(MovementEventType.WithdrawalPaid)]
    [InlineData(MovementEventType.PaymentOrderProcessed)]
    [InlineData(MovementEventType.PaymentOrderProcess)]
    [InlineData(MovementEventType.PaymentOrderPaidReversed)]
    public void Classify_PayOutEventTypes_ShouldReturnPayOut(MovementEventType eventType)
    {
        var result = MovementClassifier.Classify(eventType);

        result.Should().Be(MovementDirection.PayOut);
    }

    [Fact]
    public void Classify_UnknownEventType_ShouldThrowDomainException()
    {
        var unknownType = (MovementEventType)9999;

        var act = () => MovementClassifier.Classify(unknownType);

        act.Should().Throw<DomainException>()
            .WithMessage("*Unknown MovementEventType*");
    }

    [Fact]
    public void Classify_AllDefinedEventTypes_ShouldBeMapped()
    {
        var allEventTypes = Enum.GetValues<MovementEventType>();

        foreach (var eventType in allEventTypes)
        {
            var act = () => MovementClassifier.Classify(eventType);
            act.Should().NotThrow<DomainException>(
                $"MovementEventType.{eventType} should be mapped in MovementClassifier");
        }
    }
}
