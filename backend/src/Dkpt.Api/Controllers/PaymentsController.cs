using Dkpt.Application.Common.DTOs;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Enums;
using Dkpt.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepo;

    public PaymentsController(IPaymentRepository paymentRepo) => _paymentRepo = paymentRepo;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? year = null,
        [FromQuery] string? method = null,
        [FromQuery] Guid? memberId = null,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _paymentRepo.GetPagedAsync(
            page, pageSize, search, year, method, memberId, ct);

        var dtos = items.Select(p => new PaymentDto(
            p.Id, p.MemberId, p.Annee, p.DatePaiement,
            p.Montant, p.FraisPaiement,
            p.MoyenPaiement.ToString(), p.Reference, p.Note,
            p.CreatedAt,
            p.Member != null
                ? new MemberSimpleDto(p.Member.Id, p.Member.NumeroMembre, p.Member.Prenom, p.Member.Nom)
                : null
        )).ToList();

        return Ok(new PagedResult<PaymentDto>(dtos, totalCount, page, pageSize));
    }

    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetByMember(Guid memberId, CancellationToken ct)
    {
        var items = await _paymentRepo.GetByMemberIdAsync(memberId, ct);
        var dtos = items.Select(p => new PaymentDto(
            p.Id, p.MemberId, p.Annee, p.DatePaiement,
            p.Montant, p.FraisPaiement,
            p.MoyenPaiement.ToString(), p.Reference, p.Note,
            p.CreatedAt, null));
        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretaire,Tresorier")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest req, CancellationToken ct)
    {
        var payment = new Payment
        {
            MemberId = req.MemberId,
            Annee = req.Annee,
            DatePaiement = req.DatePaiement,
            Montant = req.Montant,
            FraisPaiement = req.FraisPaiement,
            MoyenPaiement = req.MoyenPaiement,
            Reference = req.Reference,
            Note = req.Note
        };

        await _paymentRepo.AddAsync(payment, ct);
        return Created("", new { id = payment.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentRequest req, CancellationToken ct)
    {
        var payment = await _paymentRepo.GetByIdAsync(id, ct);
        if (payment is null) return NotFound();

        if (req.MemberId.HasValue) payment.MemberId = req.MemberId.Value;
        if (req.Annee.HasValue) payment.Annee = req.Annee.Value;
        if (req.DatePaiement.HasValue) payment.DatePaiement = req.DatePaiement.Value;
        if (req.Montant.HasValue) payment.Montant = req.Montant.Value;
        if (req.FraisPaiement.HasValue) payment.FraisPaiement = req.FraisPaiement.Value;
        if (req.MoyenPaiement.HasValue) payment.MoyenPaiement = req.MoyenPaiement.Value;
        if (req.Reference is not null) payment.Reference = req.Reference;
        if (req.Note is not null) payment.Note = req.Note;

        await _paymentRepo.UpdateAsync(payment, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Secretaire")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _paymentRepo.DeleteAsync(id, ct);
        return NoContent();
    }
}
