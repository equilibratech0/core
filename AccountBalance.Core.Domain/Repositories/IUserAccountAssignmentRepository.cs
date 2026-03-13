namespace AccountBalance.Core.Domain.Repositories;

public interface IUserAccountAssignmentRepository
{
    Task AssignAccountToAdminUsersAsync(Guid companyId, Guid accountId, CancellationToken cancellationToken = default);
}
