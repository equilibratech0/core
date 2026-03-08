namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Entities;
using AccountBalance.Core.Domain.Repositories;

public class CompanyAccountMappingRepository : ICompanyAccountMappingRepository
{
    private const string CollectionName = "company_account";

    private readonly IMongoDbContext _dbContext;
    private readonly IMongoCollection<CompanyAccountMapping> _collection;
    private readonly ILogger<CompanyAccountMappingRepository> _logger;

    public CompanyAccountMappingRepository(IMongoDbContext dbContext, ILogger<CompanyAccountMappingRepository> logger)
    {
        _dbContext = dbContext;
        _collection = dbContext.GetCollection<CompanyAccountMapping>(CollectionName);
        _logger = logger;
    }

    public async Task UpsertAsync(CompanyAccountMapping mapping, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting CompanyAccountMapping for Company {CompanyId}, Account {AccountId}",
            mapping.CompanyId, mapping.AccountId);

        var filter = Builders<CompanyAccountMapping>.Filter.Eq(m => m.CompanyId, mapping.CompanyId);
        var options = new ReplaceOptions { IsUpsert = true };

        if (_dbContext.Session is not null)
            await _collection.ReplaceOneAsync(_dbContext.Session, filter, mapping, options, cancellationToken);
        else
            await _collection.ReplaceOneAsync(filter, mapping, options, cancellationToken);
    }
}
