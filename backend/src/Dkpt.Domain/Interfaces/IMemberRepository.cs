using Dkpt.Domain.Entities;

namespace Dkpt.Domain.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Member> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? searchTerm = null,
        string? village = null,
        string? status = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Member>> GetAllSimpleAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetDistinctVillagesAsync(CancellationToken ct = default);
    Task<Member> AddAsync(Member member, CancellationToken ct = default);
    Task UpdateAsync(Member member, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
