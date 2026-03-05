namespace AccountBalance.Core.Domain.Entities;

/// <summary>
/// Tracks which source events have been processed to guarantee idempotency.
/// Stored as an immutable record — never updated or deleted.
/// </summary>
public class ProcessedEvent
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public Guid SourceTransactionId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(string idempotencyKey, Guid sourceTransactionId)
    {
        Id = Guid.NewGuid();
        IdempotencyKey = idempotencyKey ?? throw new ArgumentNullException(nameof(idempotencyKey));
        SourceTransactionId = sourceTransactionId;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
