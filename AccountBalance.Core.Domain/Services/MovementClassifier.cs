namespace AccountBalance.Core.Domain.Services;

using Shared.Domain.Enums;
using Shared.Domain.Exceptions;
using AccountBalance.Core.Domain.Enums;

public static class MovementClassifier
{
    public static MovementDirection Classify(MovementEventType eventType)
    {
        return eventType switch
        {
            MovementEventType.TransactionApproved       => MovementDirection.PayIn,
            MovementEventType.TopupCreated              => MovementDirection.PayIn,
            MovementEventType.AdjustmentTopupCreated    => MovementDirection.PayIn,
            MovementEventType.AdjustmentRebateFeeCreated => MovementDirection.PayIn,
            MovementEventType.RollingReserveReleased    => MovementDirection.PayIn,
            MovementEventType.ClaimClose                => MovementDirection.PayIn,
            MovementEventType.ChargebackClose           => MovementDirection.PayIn,
            MovementEventType.PayoutError               => MovementDirection.PayIn,
            MovementEventType.WithdrawalCancelled       => MovementDirection.PayIn,
            MovementEventType.WithdrawalReturned        => MovementDirection.PayIn,
            MovementEventType.PartialPayment            => MovementDirection.PayIn,

            MovementEventType.PayoutFinished            => MovementDirection.PayOut,
            MovementEventType.ClaimRefund               => MovementDirection.PayOut,
            MovementEventType.ChargebackRefund          => MovementDirection.PayOut,
            MovementEventType.SettlementPublished       => MovementDirection.PayOut,
            MovementEventType.AccountSettlement         => MovementDirection.PayOut,
            MovementEventType.AdjustmentCreated         => MovementDirection.PayOut,
            MovementEventType.AdjustmentRollingReserveCreated => MovementDirection.PayOut,
            MovementEventType.AdjustmentBalanceFeeCreated => MovementDirection.PayOut,
            MovementEventType.WithdrawalPaid            => MovementDirection.PayOut,

            _ => throw new DomainException($"Unknown MovementEventType: {eventType}")
        };
    }
}
