namespace Steward.Application.Tracking.Registrations;

public record RegistrationResponse(
    Guid Id,
    Guid AssetId,
    string RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes,
    bool HasDocument,
    string? DocumentUrl);

public record CreateRegistrationRequest(
    string RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes);

public record UpdateRegistrationRequest(
    string RegistrationNumber,
    string? IssuingAuthority,
    DateOnly? RenewedOn,
    decimal? Cost,
    DateOnly? ExpiresOn,
    string? Notes);
