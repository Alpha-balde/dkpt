using Dkpt.Domain.Enums;

namespace Dkpt.Application.Common.DTOs;

// === Auth ===
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email, string Role);
public record RegisterRequest(string Email, string Password, UserRole Role = UserRole.Lecteur);

// === Members ===
public record MemberDto(
    Guid Id, string NumeroMembre, string Prenom, string Nom,
    string? Telephone, string? WhatsApp, string? Residence,
    string? Village, string? SousPrefecture,
    int AnneeDebut, bool Actif, DateTime CreatedAt);

public record MemberSimpleDto(Guid Id, string NumeroMembre, string Prenom, string Nom);

public record CreateMemberRequest(
    string NumeroMembre, string Prenom, string Nom,
    string? Telephone, string? WhatsApp, string? Residence,
    string? Village, string? SousPrefecture,
    int? AnneeDebut, bool Actif = true);

public record UpdateMemberRequest(
    string? NumeroMembre, string? Prenom, string? Nom,
    string? Telephone, string? WhatsApp, string? Residence,
    string? Village, string? SousPrefecture,
    int? AnneeDebut, bool? Actif);

// === Payments ===
public record PaymentDto(
    Guid Id, Guid MemberId, int Annee, DateOnly DatePaiement,
    decimal Montant, decimal FraisPaiement,
    string MoyenPaiement, string? Reference, string? Note,
    DateTime CreatedAt, MemberSimpleDto? Member);

public record CreatePaymentRequest(
    Guid MemberId, int Annee, DateOnly DatePaiement,
    decimal Montant, decimal FraisPaiement,
    PaymentMethod MoyenPaiement, string? Reference, string? Note);

public record UpdatePaymentRequest(
    Guid? MemberId, int? Annee, DateOnly? DatePaiement,
    decimal? Montant, decimal? FraisPaiement,
    PaymentMethod? MoyenPaiement, string? Reference, string? Note);

// === Contribution Amounts ===
public record ContributionAmountDto(int Year, int Amount);
public record CreateContributionAmountRequest(int Year, int Amount);
public record UpdateContributionAmountRequest(int Amount);

// === Users ===
public record UserDto(Guid Id, string Email, string Role, DateTime CreatedAt);
public record CreateUserRequest(string Email, string Password, UserRole Role = UserRole.Lecteur);
public record UpdateUserRequest(string? Email, UserRole? Role, string? Password);

// === Dashboard ===
public record DashboardStatsDto(
    int TotalMembers, decimal TotalCollected, decimal TotalFees,
    int MembersInOrder, int MembersNotInOrder);

public record EncaissementStatsDto(
    decimal TotalEncaisse, decimal TotalFrais, int NombrePaiements);

public record MonthlyEncaissementDto(string Month, decimal Montant);

// === Cotisations ===
public record CotisationStatDto(
    Guid Id, string NumeroMembre, string Prenom, string Nom,
    decimal PaidAmount, decimal ExpectedAmount, decimal Gap, string Status);

// === Arriérés ===
public record ArriereDto(
    Guid Id, string NumeroMembre, string Prenom, string Nom,
    decimal ExpectedAmount, decimal PaidAmount, decimal Gap);

// === Common ===
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
