namespace Steward.Application.Tracking.Warranties;

public record WarrantyResponse(
    Guid Id,
    Guid AssetId,
    string Provider,
    string? Description,
    DateOnly? StartsOn,
    DateOnly? ExpiresOn,
    string? Notes,
    bool HasDocument,
    string? DocumentUrl);

public record CreateWarrantyRequest(
    string Provider,
    string? Description,
    DateOnly? StartsOn,
    DateOnly? ExpiresOn,
    string? Notes);

public record UpdateWarrantyRequest(
    string Provider,
    string? Description,
    DateOnly? StartsOn,
    DateOnly? ExpiresOn,
    string? Notes);
