namespace AccountBalance.Core.Application.Commands;

using Shared.Domain.Enums;

public record ProcessTransactionCommand(
    Guid TransactionId,
    Guid ClientId,
    string ClientName,
    string IdempotencyKey,
    MovementEventType EventType,
    string RawPayload);
