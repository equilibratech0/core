namespace AccountBalance.Core.Domain.Entities;

public class ProcessedEvent
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public ProcessedEvent(Guid transactionId)
    {
        Id = Guid.NewGuid();
        TransactionId = transactionId;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
