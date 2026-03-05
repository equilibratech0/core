namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Repositories;

public class ClientAccountMappingRepository : IClientAccountMappingRepository
{
    private const string CollectionName = "client_account";

    private readonly IMongoDbContext _dbContext;
    private readonly IMongoCollection<ClientAccountMapping> _collection;
    private readonly ILogger<ClientAccountMappingRepository> _logger;

    public ClientAccountMappingRepository(IMongoDbContext dbContext, ILogger<ClientAccountMappingRepository> logger)
    {
        _dbContext = dbContext;
        _collection = dbContext.GetCollection<ClientAccountMapping>(CollectionName);
        _logger = logger;
    }

    public async Task<ClientAccountMapping?> GetByClientIdAsync(
        Guid clientId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ClientAccountMapping>.Filter.Eq(m => m.ClientId, clientId);

        if (_dbContext.Session is not null)
            return await _collection.Find(_dbContext.Session, filter).SingleOrDefaultAsync(cancellationToken);

        return await _collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ClientAccountMapping mapping, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating ClientAccountMapping for Client {ClientId}, Account {AccountId}",
            mapping.ClientId, mapping.AccountId);

        if (_dbContext.Session is not null)
            await _collection.InsertOneAsync(_dbContext.Session, mapping, cancellationToken: cancellationToken);
        else
            await _collection.InsertOneAsync(mapping, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(ClientAccountMapping mapping, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating ClientAccountMapping {Id} for Client {ClientId}, Account {AccountId}",
            mapping.Id, mapping.ClientId, mapping.AccountId);

        var filter = Builders<ClientAccountMapping>.Filter.Eq(m => m.Id, mapping.Id);

        if (_dbContext.Session is not null)
            await _collection.ReplaceOneAsync(_dbContext.Session, filter, mapping, cancellationToken: cancellationToken);
        else
            await _collection.ReplaceOneAsync(filter, mapping, cancellationToken: cancellationToken);
    }
}
