namespace AccountBalance.Core.Domain.Repositories;

public interface IUserAccountAssignmentRepository
{
    Task AssignAccountToAdminUsersAsync(Guid companyId, string accountId, CancellationToken cancellationToken = default);
}
