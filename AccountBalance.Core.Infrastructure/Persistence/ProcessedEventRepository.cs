namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Repositories;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private const string CollectionName = "processed_events";

    private readonly IMongoDbContext _dbContext;
    private readonly IMongoCollection<ProcessedEvent> _collection;
    private readonly ILogger<ProcessedEventRepository> _logger;

    public ProcessedEventRepository(IMongoDbContext dbContext, ILogger<ProcessedEventRepository> logger)
    {
        _dbContext = dbContext;
        _collection = dbContext.GetCollection<ProcessedEvent>(CollectionName);
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ProcessedEvent>.Filter.Eq(e => e.IdempotencyKey, idempotencyKey);
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Recording ProcessedEvent for IdempotencyKey {Key}", processedEvent.IdempotencyKey);

        if (_dbContext.Session is not null)
            await _collection.InsertOneAsync(_dbContext.Session, processedEvent, cancellationToken: cancellationToken);
        else
            await _collection.InsertOneAsync(processedEvent, cancellationToken: cancellationToken);
    }
}
