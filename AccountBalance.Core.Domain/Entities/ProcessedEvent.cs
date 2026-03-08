namespace AccountBalance.Core.Domain.Entities;

using Shared.Domain.Enums;

public class ProcessedEvent
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public MovementEventType EventType { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid transactionId, MovementEventType eventType)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        EventType = eventType;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
