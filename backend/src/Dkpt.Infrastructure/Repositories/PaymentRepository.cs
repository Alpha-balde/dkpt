using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Dkpt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dkpt.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _db;

    public PaymentRepository(ApplicationDbContext db) => _db = db;

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Payments.Include(p => p.Member).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        int? year = null, string? method = null,
        Guid? memberId = null, CancellationToken ct = default)
    {
        var query = _db.Payments.Include(p => p.Member).AsQueryable();

        if (year.HasValue)
            query = query.Where(p => p.Annee == year.Value);

        if (!string.IsNullOrWhiteSpace(method) && method != "all")
            query = query.Where(p => p.MoyenPaiement.ToString() == method);

        if (memberId.HasValue)
            query = query.Where(p => p.MemberId == memberId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Member.Prenom.ToLower().Contains(term) ||
                p.Member.Nom.ToLower().Contains(term) ||
                p.Member.NumeroMembre.ToLower().Contains(term) ||
                (p.Reference != null && p.Reference.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.DatePaiement)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Payment>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
        => await _db.Payments
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.Annee)
            .ThenByDescending(p => p.DatePaiement)
            .ToListAsync(ct);

    public async Task<Payment> AddAsync(Payment payment, CancellationToken ct = default)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _db.Payments.FindAsync([id], ct);
        if (payment is not null)
        {
            _db.Payments.Remove(payment);
            await _db.SaveChangesAsync(ct);
        }
    }
}
