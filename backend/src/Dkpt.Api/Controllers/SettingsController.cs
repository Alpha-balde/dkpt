using Dkpt.Application.Common.DTOs;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly IContributionAmountRepository _repo;

    public SettingsController(IContributionAmountRepository repo) => _repo = repo;

    [HttpGet("contributions")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        var dtos = items.Select(c => new ContributionAmountDto(c.Year, c.Amount));
        return Ok(dtos);
    }

    [HttpGet("years")]
    [AllowAnonymous]
    public async Task<IActionResult> GetYears(CancellationToken ct)
    {
        var years = await _repo.GetAvailableYearsAsync(ct);
        return Ok(years);
    }

    [HttpPost("contributions")]
    public async Task<IActionResult> Create([FromBody] CreateContributionAmountRequest req, CancellationToken ct)
    {
        var existing = await _repo.GetByYearAsync(req.Year, ct);
        if (existing is not null)
            return Conflict(new { message = $"Un montant existe déjà pour {req.Year}." });

        var amount = new ContributionAmount { Year = req.Year, Amount = req.Amount };
        await _repo.AddAsync(amount, ct);
        return Created("", new ContributionAmountDto(amount.Year, amount.Amount));
    }

    [HttpPut("contributions/{year:int}")]
    public async Task<IActionResult> Update(int year, [FromBody] UpdateContributionAmountRequest req, CancellationToken ct)
    {
        var amount = await _repo.GetByYearAsync(year, ct);
        if (amount is null) return NotFound();

        amount.Amount = req.Amount;
        await _repo.UpdateAsync(amount, ct);
        return NoContent();
    }

    [HttpDelete("contributions/{year:int}")]
    public async Task<IActionResult> Delete(int year, CancellationToken ct)
    {
        await _repo.DeleteAsync(year, ct);
        return NoContent();
    }
}
