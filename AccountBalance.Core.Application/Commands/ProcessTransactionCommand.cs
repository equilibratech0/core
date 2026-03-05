namespace AccountBalance.Core.Application.Commands;

using System.Collections.Generic;
using Shared.Domain.Enums;

public record ProcessTransactionCommand(
    Guid TransactionId,
    Guid ClientId,
    string ClientName,
    IReadOnlyList<string> UserIds,
    string IdempotencyKey,
    MovementEventType EventType,
    string RawPayload);
