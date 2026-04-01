using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Dkpt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dkpt.Infrastructure.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly ApplicationDbContext _db;

    public MemberRepository(ApplicationDbContext db) => _db = db;

    public async Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Members.Include(m => m.Payments).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<(IReadOnlyList<Member> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        string? village = null, string? status = null, CancellationToken ct = default)
    {
        var query = _db.Members.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(m =>
                m.Prenom.ToLower().Contains(term) ||
                m.Nom.ToLower().Contains(term) ||
                m.NumeroMembre.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(village) && village != "all")
            query = query.Where(m => m.Village == village);

        var normalizedStatus = status?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStatus) &&
            !normalizedStatus.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (normalizedStatus.Equals("Actif", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => m.Actif);
            else if (normalizedStatus.Equals("Inactif", StringComparison.OrdinalIgnoreCase))
                query = query.Where(m => !m.Actif);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.Nom).ThenBy(m => m.Prenom)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Member>> GetAllSimpleAsync(CancellationToken ct = default)
        => await _db.Members
            .Where(m => m.Actif)
            .OrderBy(m => m.Nom).ThenBy(m => m.Prenom)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetDistinctVillagesAsync(CancellationToken ct = default)
        => await _db.Members
            .Where(m => !string.IsNullOrEmpty(m.Village))
            .Select(m => m.Village!)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync(ct);

    public async Task<Member> AddAsync(Member member, CancellationToken ct = default)
    {
        _db.Members.Add(member);
        await _db.SaveChangesAsync(ct);
        return member;
    }

    public async Task UpdateAsync(Member member, CancellationToken ct = default)
    {
        _db.Members.Update(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var member = await _db.Members.FindAsync([id], ct);
        if (member is not null)
        {
            _db.Members.Remove(member);
            await _db.SaveChangesAsync(ct);
        }
    }
}
