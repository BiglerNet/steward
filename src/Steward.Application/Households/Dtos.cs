namespace Steward.Application.Households;

public record CreateHouseholdRequest(
    string Name, string PublicSlug, bool IsPublicVisible, string? Country, string? Region) : IHouseholdLocation;

public record UpdateHouseholdRequest(
    string Name, string PublicSlug, bool IsPublicVisible, string? Country, string? Region) : IHouseholdLocation;

public record HouseholdResponse(
    Guid Id,
    string Name,
    string PublicSlug,
    bool IsPublicVisible,
    string? Country,
    string? Region,
    string UserRole,
    long StorageUsedBytes,
    long StorageQuotaBytes,
    DateTimeOffset CreatedAt);
