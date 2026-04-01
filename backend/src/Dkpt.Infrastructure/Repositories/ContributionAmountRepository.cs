using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Dkpt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dkpt.Infrastructure.Repositories;

public class ContributionAmountRepository : IContributionAmountRepository
{
    private readonly ApplicationDbContext _db;

    public ContributionAmountRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContributionAmount>> GetAllAsync(CancellationToken ct = default)
        => await _db.ContributionAmounts.OrderByDescending(c => c.Year).ToListAsync(ct);

    public async Task<ContributionAmount?> GetByYearAsync(int year, CancellationToken ct = default)
        => await _db.ContributionAmounts.FindAsync([year], ct);

    public async Task<IReadOnlyList<int>> GetAvailableYearsAsync(CancellationToken ct = default)
        => await _db.ContributionAmounts
            .Select(c => c.Year)
            .OrderByDescending(y => y)
            .ToListAsync(ct);

    public async Task<ContributionAmount> AddAsync(ContributionAmount amount, CancellationToken ct = default)
    {
        _db.ContributionAmounts.Add(amount);
        await _db.SaveChangesAsync(ct);
        return amount;
    }

    public async Task UpdateAsync(ContributionAmount amount, CancellationToken ct = default)
    {
        _db.ContributionAmounts.Update(amount);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int year, CancellationToken ct = default)
    {
        var amount = await _db.ContributionAmounts.FindAsync([year], ct);
        if (amount is not null)
        {
            _db.ContributionAmounts.Remove(amount);
            await _db.SaveChangesAsync(ct);
        }
    }
}
