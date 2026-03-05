namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Domain.Enums;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.Repositories;
using AccountBalance.Core.Domain.ValueObjects;

public class AccountBalanceRepository : IAccountBalanceRepository
{
    private const string CollectionName = "account_balances";

    private readonly IMongoDbContext _dbContext;
    private readonly IMongoCollection<AccountBalanceEntry> _collection;
    private readonly ILogger<AccountBalanceRepository> _logger;

    public AccountBalanceRepository(IMongoDbContext dbContext, ILogger<AccountBalanceRepository> logger)
    {
        _dbContext = dbContext;
        _collection = dbContext.GetCollection<AccountBalanceEntry>(CollectionName);
        _logger = logger;
    }

    public async Task<AccountBalanceEntry?> GetByAccountAndCurrencyAsync(
        Guid clientId, string accountId, Currency currency, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AccountBalanceEntry>.Filter.And(
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.ClientId, clientId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.AccountId, accountId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.Currency, currency));

        if (_dbContext.Session is not null)
            return await _collection.Find(_dbContext.Session, filter).SingleOrDefaultAsync(cancellationToken);

        return await _collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating AccountBalance for Client {ClientId}, Account {AccountId}, Currency {Currency}",
            entry.ClientId, entry.AccountId, entry.Currency);

        if (_dbContext.Session is not null)
            await _collection.InsertOneAsync(_dbContext.Session, entry, cancellationToken: cancellationToken);
        else
            await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating AccountBalance {Id}, new AvailableBalance={Balance}",
            entry.Id, entry.AvailableBalance);

        var filter = Builders<AccountBalanceEntry>.Filter.Eq(b => b.Id, entry.Id);

        if (_dbContext.Session is not null)
            await _collection.ReplaceOneAsync(_dbContext.Session, filter, entry, cancellationToken: cancellationToken);
        else
            await _collection.ReplaceOneAsync(filter, entry, cancellationToken: cancellationToken);
    }
}
