using Dkpt.Domain.Entities;

namespace Dkpt.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? searchTerm = null,
        int? year = null,
        string? method = null,
        Guid? memberId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken ct = default);
    Task UpdateAsync(Payment payment, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
