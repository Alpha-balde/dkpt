using Dkpt.Application.Common.DTOs;
using Dkpt.Domain.Entities;
using Dkpt.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CotisationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CotisationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? year = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var setting = await GetOrCreateSettingAsync(ct);
        var defaultAmount = setting.MontantCotisationAnnuelleParDefaut;
        var currentYear = DateTime.UtcNow.Year;

        var members = await _db.Members
            .AsNoTracking()
            .OrderBy(m => m.Nom)
            .ThenBy(m => m.Prenom)
            .ToListAsync(ct);

        var paymentsQuery = _db.Payments.AsNoTracking();
        if (year.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.Annee == year.Value);

        var payments = await paymentsQuery.ToListAsync(ct);
        var paymentsByMember = payments
            .GroupBy(p => p.MemberId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var amounts = await _db.ContributionAmounts
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Year, c => (decimal)c.Amount, ct);

        decimal GetAmountForYear(int targetYear) =>
            amounts.TryGetValue(targetYear, out var amount) ? amount : defaultAmount;

        var rows = members.Select(member =>
        {
            var paidAmount = paymentsByMember.TryGetValue(member.Id, out var memberPayments)
                ? memberPayments.Sum(p => p.Montant)
                : 0m;

            decimal expectedAmount;
            if (year.HasValue)
            {
                expectedAmount = year.Value < member.AnneeDebut ? 0m : GetAmountForYear(year.Value);
            }
            else
            {
                expectedAmount = 0m;
                if (member.AnneeDebut <= currentYear)
                {
                    for (var targetYear = member.AnneeDebut; targetYear <= currentYear; targetYear++)
                        expectedAmount += GetAmountForYear(targetYear);
                }
            }

            var gap = Math.Max(expectedAmount - paidAmount, 0m);
            var computedStatus = gap == 0m ? "En ordre" : "Pas en ordre";

            return new CotisationStatDto(
                member.Id,
                member.NumeroMembre,
                member.Prenom,
                member.Nom,
                paidAmount,
                expectedAmount,
                gap,
                computedStatus);
        });

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            rows = rows.Where(row => string.Equals(row.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            rows = rows.Where(row =>
                row.Prenom.ToLowerInvariant().Contains(normalizedSearch) ||
                row.Nom.ToLowerInvariant().Contains(normalizedSearch) ||
                row.NumeroMembre.ToLowerInvariant().Contains(normalizedSearch));
        }

        var filteredRows = rows.ToList();
        var totalCount = filteredRows.Count;
        var items = filteredRows
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new PagedResult<CotisationStatDto>(items, totalCount, page, pageSize));
    }

    private async Task<Setting> GetOrCreateSettingAsync(CancellationToken ct)
    {
        var setting = await _db.Settings.SingleOrDefaultAsync(ct);
        if (setting is not null)
            return setting;

        setting = new Setting { Id = 1 };
        _db.Settings.Add(setting);
        await _db.SaveChangesAsync(ct);
        return setting;
    }
}
