using Steward.Application.Common;
using Steward.Domain.Enums;

namespace Steward.Application.Tracking.MaintenanceItems;

public record MaintenanceItemResponse(
    Guid Id,
    Guid AssetId,
    Guid? EngineId,
    Guid? TemplateId,
    string Title,
    string? Description,
    string? ProviderName,
    MaintenanceItemStatus Status,
    DateOnly? Date,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    bool IsBlocked,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ChecklistItemResponse> ChecklistItems,
    IReadOnlyList<PartLineResponse> PartLines);

public record HouseholdMaintenanceItemResponse(
    Guid Id,
    Guid AssetId,
    string AssetName,
    Guid? EngineId,
    Guid? TemplateId,
    string Title,
    string? Description,
    string? ProviderName,
    MaintenanceItemStatus Status,
    DateOnly? Date,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    bool IsBlocked,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ChecklistItemResponse> ChecklistItems,
    IReadOnlyList<PartLineResponse> PartLines);

public record CreateMaintenanceItemRequest(
    string Title,
    string? Description,
    string? ProviderName,
    MaintenanceItemStatus? Status,
    DateOnly? Date,
    decimal? Cost,
    decimal? OdometerMiles,
    decimal? EngineHours,
    Guid? EngineId,
    Guid? TemplateId);

public record PatchMaintenanceItemRequest(
    Optional<string> Title,
    Optional<string?> Description,
    Optional<string?> ProviderName,
    Optional<MaintenanceItemStatus> Status,
    Optional<DateOnly?> Date,
    Optional<decimal?> Cost,
    Optional<decimal?> OdometerMiles,
    Optional<decimal?> EngineHours,
    Optional<Guid?> EngineId);

public record ChecklistItemResponse(
    Guid Id,
    Guid MaintenanceItemId,
    string Text,
    ChecklistItemStatus Status,
    DateTimeOffset? ResolvedAt,
    int SortOrder,
    Guid? EngineId,
    Guid? TemplateStepId);

public record CreateChecklistItemRequest(string Text, Guid? EngineId);

public record PatchChecklistItemRequest(
    Optional<string> Text,
    Optional<ChecklistItemStatus> Status,
    Optional<Guid?> EngineId);

public record ReorderChecklistItemsRequest(IReadOnlyList<Guid> ChecklistItemIds);

public record PartLineResponse(
    Guid Id,
    Guid MaintenanceItemId,
    string Name,
    string? PartNumber,
    string? Vendor,
    string? TrackingNumber,
    string? OrderUrl,
    decimal Quantity,
    PartLineStatus Status,
    decimal? Cost,
    Guid? ChecklistItemId,
    Guid? PartId);

public record CreatePartLineRequest(
    string Name,
    string? PartNumber,
    string? Vendor,
    string? TrackingNumber,
    string? OrderUrl,
    decimal? Quantity,
    decimal? Cost,
    Guid? ChecklistItemId);

public record PatchPartLineRequest(
    Optional<string> Name,
    Optional<string?> PartNumber,
    Optional<string?> Vendor,
    Optional<string?> TrackingNumber,
    Optional<string?> OrderUrl,
    Optional<decimal> Quantity,
    Optional<PartLineStatus> Status,
    Optional<decimal?> Cost,
    Optional<Guid?> ChecklistItemId);
