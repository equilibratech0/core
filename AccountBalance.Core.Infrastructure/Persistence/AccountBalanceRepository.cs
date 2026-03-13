namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Domain.Enums;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Aggregates;
using AccountBalance.Core.Domain.Repositories;

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

    public async Task<AccountBalanceEntry?> GetByAccountAsync(
        Guid companyId, Guid accountId, Currency currency, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AccountBalanceEntry>.Filter.And(
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.CompanyId, companyId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.AccountId, accountId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.Currency, currency));

        if (_dbContext.Session is not null)
            return await _collection.Find(_dbContext.Session, filter).SingleOrDefaultAsync(cancellationToken);

        return await _collection.Find(filter).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(AccountBalanceEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting AccountBalance for Company {CompanyId}, Account {AccountId}, Balance={Balance}",
            entry.CompanyId, entry.AccountId, entry.AvailableBalance);

        var filter = Builders<AccountBalanceEntry>.Filter.And(
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.CompanyId, entry.CompanyId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.AccountId, entry.AccountId),
            Builders<AccountBalanceEntry>.Filter.Eq(b => b.Currency, entry.Currency));

        var options = new ReplaceOptions { IsUpsert = true };

        if (_dbContext.Session is not null)
            await _collection.ReplaceOneAsync(_dbContext.Session, filter, entry, options, cancellationToken);
        else
            await _collection.ReplaceOneAsync(filter, entry, options, cancellationToken);
    }
}
