namespace AccountBalance.Core.Domain.Services;

using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.Enums;

/// <summary>
/// Pure domain service that determines balance impact direction for each event type.
/// </summary>
public static class MovementClassifier
{
    public static MovementDirection Classify(MovementEventType eventType)
    {
        return eventType switch
        {
            // PayIn — money flowing into the account
            MovementEventType.TransactionCreated        => MovementDirection.PayIn,
            MovementEventType.TransactionApproved       => MovementDirection.PayIn,
            MovementEventType.TopupCreated              => MovementDirection.PayIn,
            MovementEventType.AdjustmentTopupCreated    => MovementDirection.PayIn,
            MovementEventType.AdjustmentRebateFeeCreated => MovementDirection.PayIn,
            MovementEventType.RollingReserveReleased    => MovementDirection.PayIn,
            MovementEventType.ClaimClose                => MovementDirection.PayIn,
            MovementEventType.ClaimCloseModified        => MovementDirection.PayIn,
            MovementEventType.ChargebackClose           => MovementDirection.PayIn,
            MovementEventType.ChargebackCloseModified   => MovementDirection.PayIn,
            MovementEventType.PaymentOrderPaid          => MovementDirection.PayIn,
            MovementEventType.PaymentOrderCancelled     => MovementDirection.PayIn,
            MovementEventType.WithdrawalCancelled       => MovementDirection.PayIn,
            MovementEventType.WithdrawalReturned        => MovementDirection.PayIn,
            MovementEventType.PayoutError               => MovementDirection.PayIn,
            MovementEventType.SettlementUnpublished     => MovementDirection.PayIn,
            MovementEventType.PartialPayment            => MovementDirection.PayIn,

            // PayOut — money flowing out of the account
            MovementEventType.PayoutCreated             => MovementDirection.PayOut,
            MovementEventType.PayoutFinished            => MovementDirection.PayOut,
            MovementEventType.PayoutFinishedRefund      => MovementDirection.PayOut,
            MovementEventType.ClaimOpen                 => MovementDirection.PayOut,
            MovementEventType.ClaimRefund               => MovementDirection.PayOut,
            MovementEventType.ClaimReopen               => MovementDirection.PayOut,
            MovementEventType.ChargebackOpen            => MovementDirection.PayOut,
            MovementEventType.ChargebackReopen          => MovementDirection.PayOut,
            MovementEventType.ChargebackRefund          => MovementDirection.PayOut,
            MovementEventType.SettlementPublished       => MovementDirection.PayOut,
            MovementEventType.AccountSettlement         => MovementDirection.PayOut,
            MovementEventType.AdjustmentCreated         => MovementDirection.PayOut,
            MovementEventType.AdjustmentRollingReserveCreated => MovementDirection.PayOut,
            MovementEventType.AdjustmentBalanceFeeCreated => MovementDirection.PayOut,
            MovementEventType.WithdrawalApproved        => MovementDirection.PayOut,
            MovementEventType.WithdrawalPaid            => MovementDirection.PayOut,
            MovementEventType.PaymentOrderProcessed     => MovementDirection.PayOut,
            MovementEventType.PaymentOrderProcess       => MovementDirection.PayOut,
            MovementEventType.PaymentOrderPaidReversed  => MovementDirection.PayOut,

            _ => throw new DomainException($"Unknown MovementEventType: {eventType}")
        };
    }
}
