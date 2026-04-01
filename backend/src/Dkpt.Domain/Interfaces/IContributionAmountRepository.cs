using Dkpt.Domain.Entities;

namespace Dkpt.Domain.Interfaces;

public interface IContributionAmountRepository
{
    Task<IReadOnlyList<ContributionAmount>> GetAllAsync(CancellationToken ct = default);
    Task<ContributionAmount?> GetByYearAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetAvailableYearsAsync(CancellationToken ct = default);
    Task<ContributionAmount> AddAsync(ContributionAmount amount, CancellationToken ct = default);
    Task UpdateAsync(ContributionAmount amount, CancellationToken ct = default);
    Task DeleteAsync(int year, CancellationToken ct = default);
}
