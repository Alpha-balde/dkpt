using Dkpt.Application.Common.DTOs;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMemberRepository _memberRepo;

    public MembersController(IMemberRepository memberRepo) => _memberRepo = memberRepo;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? village = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _memberRepo.GetPagedAsync(page, pageSize, search, village, status, ct);

        var dtos = items.Select(m => new MemberDto(
            m.Id, m.NumeroMembre, m.Prenom, m.Nom,
            m.Telephone, m.WhatsApp, m.Residence,
            m.Village, m.SousPrefecture,
            m.AnneeDebut, m.Actif, m.CreatedAt)).ToList();

        return Ok(new PagedResult<MemberDto>(dtos, totalCount, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var member = await _memberRepo.GetByIdAsync(id, ct);
        if (member is null) return NotFound();

        return Ok(new MemberDto(
            member.Id, member.NumeroMembre, member.Prenom, member.Nom,
            member.Telephone, member.WhatsApp, member.Residence,
            member.Village, member.SousPrefecture,
            member.AnneeDebut, member.Actif, member.CreatedAt));
    }

    [HttpGet("simple")]
    public async Task<IActionResult> GetAllSimple(CancellationToken ct)
    {
        var members = await _memberRepo.GetAllSimpleAsync(ct);
        var dtos = members.Select(m => new MemberSimpleDto(m.Id, m.NumeroMembre, m.Prenom, m.Nom));
        return Ok(dtos);
    }

    [HttpGet("villages")]
    public async Task<IActionResult> GetVillages(CancellationToken ct)
    {
        var villages = await _memberRepo.GetDistinctVillagesAsync(ct);
        return Ok(villages);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest req, CancellationToken ct)
    {
        var member = new Member
        {
            NumeroMembre = req.NumeroMembre,
            Prenom = req.Prenom,
            Nom = req.Nom,
            Telephone = req.Telephone,
            WhatsApp = req.WhatsApp,
            Residence = req.Residence,
            Village = req.Village,
            SousPrefecture = req.SousPrefecture,
            AnneeDebut = req.AnneeDebut ?? DateTime.UtcNow.Year,
            Actif = req.Actif
        };

        await _memberRepo.AddAsync(member, ct);
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, null);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMemberRequest req, CancellationToken ct)
    {
        var member = await _memberRepo.GetByIdAsync(id, ct);
        if (member is null) return NotFound();

        if (req.NumeroMembre is not null) member.NumeroMembre = req.NumeroMembre;
        if (req.Prenom is not null) member.Prenom = req.Prenom;
        if (req.Nom is not null) member.Nom = req.Nom;
        if (req.Telephone is not null) member.Telephone = req.Telephone;
        if (req.WhatsApp is not null) member.WhatsApp = req.WhatsApp;
        if (req.Residence is not null) member.Residence = req.Residence;
        if (req.Village is not null) member.Village = req.Village;
        if (req.SousPrefecture is not null) member.SousPrefecture = req.SousPrefecture;
        if (req.AnneeDebut.HasValue) member.AnneeDebut = req.AnneeDebut.Value;
        if (req.Actif.HasValue) member.Actif = req.Actif.Value;

        await _memberRepo.UpdateAsync(member, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _memberRepo.DeleteAsync(id, ct);
        return NoContent();
    }
}
