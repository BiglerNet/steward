namespace Steward.Application.PlatformAdmin;

public record AdminUserResponse(Guid Id, string Email, string? DisplayName, IReadOnlyCollection<string> Roles);

public record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

public record AssignRoleRequest(string Role);

public record SetStorageQuotaRequest(long? QuotaBytes);
