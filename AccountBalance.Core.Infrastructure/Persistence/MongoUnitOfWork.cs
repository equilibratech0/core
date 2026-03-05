namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Application.Interfaces;

public class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoDbContext _dbContext;
    private readonly ILogger<MongoUnitOfWork> _logger;

    public MongoUnitOfWork(IMongoDbContext dbContext, ILogger<MongoUnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Beginning unit of work transaction");
        await _dbContext.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Committing unit of work transaction");
        await _dbContext.CommitTransactionAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Rolling back unit of work transaction");
        await _dbContext.AbortTransactionAsync(cancellationToken);
    }
}
