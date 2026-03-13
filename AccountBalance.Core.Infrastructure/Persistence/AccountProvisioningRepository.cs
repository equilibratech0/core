namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Domain.Entities;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Repositories;

public class AccountProvisioningRepository : IAccountProvisioningRepository
{
    private readonly IMongoCollection<Account> _collection;
    private readonly ILogger<AccountProvisioningRepository> _logger;

    public AccountProvisioningRepository(IMongoDbConfigContext configContext, ILogger<AccountProvisioningRepository> logger)
    {
        _collection = configContext.GetCollection<Account>("accounts");
        _logger = logger;
    }

    public async Task<Account?> GetByReferenceAsync(Guid companyId, string accountReference, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Account>.Filter.And(
            Builders<Account>.Filter.Eq(a => a.CompanyId, companyId),
            Builders<Account>.Filter.Eq(a => a.AccountReference, accountReference));

        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(account, cancellationToken: cancellationToken);
        _logger.LogInformation("Auto-created Account {AccountId} (ref={AccountReference}) for Company {CompanyId}",
            account.Id, account.AccountReference, account.CompanyId);
    }
}
