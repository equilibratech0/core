namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Repositories;

public class MovementRepository : IMovementRepository
{
    private const string CollectionName = "movements";

    private readonly IMongoDbContext _dbContext;
    private readonly IMongoCollection<Movement> _collection;
    private readonly ILogger<MovementRepository> _logger;

    public MovementRepository(IMongoDbContext dbContext, ILogger<MovementRepository> logger)
    {
        _dbContext = dbContext;
        _collection = dbContext.GetCollection<Movement>(CollectionName);
        _logger = logger;
    }

    public async Task<bool> ExistsByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Movement>.Filter.Eq(m => m.TransactionId, transactionId);
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(Movement movement, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Inserting Movement {MovementId} with TransactionId {TransactionId}",
            movement.Id, movement.TransactionId);

        if (_dbContext.Session is not null)
            await _collection.InsertOneAsync(_dbContext.Session, movement, cancellationToken: cancellationToken);
        else
            await _collection.InsertOneAsync(movement, cancellationToken: cancellationToken);
    }
}
