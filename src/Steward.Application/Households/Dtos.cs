namespace Steward.Application.Households;

public record CreateHouseholdRequest(string Name, string PublicSlug, bool IsPublicVisible);

public record UpdateHouseholdRequest(string Name, string PublicSlug, bool IsPublicVisible);

public record HouseholdResponse(
    Guid Id,
    string Name,
    string PublicSlug,
    bool IsPublicVisible,
    string UserRole,
    DateTimeOffset CreatedAt);
