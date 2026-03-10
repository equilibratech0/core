namespace AccountBalance.Core.Infrastructure.Persistence;

using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Domain.Entities;
using Shared.Domain.Enums;
using Shared.Infrastructure.Persistence.Abstractions;
using AccountBalance.Core.Domain.Repositories;

public class UserAccountAssignmentRepository : IUserAccountAssignmentRepository
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IMongoCollection<UserAccount> _userAccountsCollection;
    private readonly ILogger<UserAccountAssignmentRepository> _logger;

    public UserAccountAssignmentRepository(IMongoDbConfigContext configContext, ILogger<UserAccountAssignmentRepository> logger)
    {
        _usersCollection = configContext.GetCollection<User>("users");
        _userAccountsCollection = configContext.GetCollection<UserAccount>("user_accounts");
        _logger = logger;
    }

    public async Task AssignAccountToAdminUsersAsync(Guid companyId, string accountId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(accountId, out var accountGuid))
            return;

        var adminFilter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.CompanyId, companyId),
            Builders<User>.Filter.Eq(u => u.AccessLevel, AccessLevel.Admin));

        var adminUsers = await _usersCollection.Find(adminFilter).ToListAsync(cancellationToken);

        foreach (var admin in adminUsers)
        {
            var filter = Builders<UserAccount>.Filter.And(
                Builders<UserAccount>.Filter.Eq(ua => ua.UserId, admin.Id),
                Builders<UserAccount>.Filter.Eq(ua => ua.AccountId, accountGuid));

            var options = new ReplaceOptions { IsUpsert = true };
            var userAccount = new UserAccount(admin.Id, accountGuid);

            await _userAccountsCollection.ReplaceOneAsync(filter, userAccount, options, cancellationToken);

            _logger.LogDebug("Account {AccountId} auto-assigned to Admin User {UserId}", accountId, admin.Id);
        }
    }
}
