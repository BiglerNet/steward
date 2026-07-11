using Steward.Domain.Enums;

namespace Steward.Application.Tracking.Registrations;

public record RegistrationResponse(
    Guid Id,
    Guid AssetId,
    RegistrationKind Kind,
    string? RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? ValidFrom,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes,
    bool HasDocument,
    string? DocumentUrl);

public record CreateRegistrationRequest(
    RegistrationKind? Kind,
    string? RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? ValidFrom,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes);

public record UpdateRegistrationRequest(
    RegistrationKind? Kind,
    string? RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? ValidFrom,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes);
