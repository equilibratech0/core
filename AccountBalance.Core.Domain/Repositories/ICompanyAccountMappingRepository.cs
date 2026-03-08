namespace AccountBalance.Core.Domain.Repositories;

using AccountBalance.Core.Domain.Entities;

public interface ICompanyAccountMappingRepository
{
    Task UpsertAsync(CompanyAccountMapping mapping, CancellationToken cancellationToken = default);
}
