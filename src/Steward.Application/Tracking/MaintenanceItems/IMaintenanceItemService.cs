using Steward.Domain.Enums;

namespace Steward.Application.Tracking.MaintenanceItems;

public interface IMaintenanceItemService
{
    Task<MaintenanceItemResponse> CreateAsync(
        Guid assetId, CreateMaintenanceItemRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MaintenanceItemResponse>> ListAsync(
        Guid assetId, IReadOnlyCollection<MaintenanceItemStatus>? statuses, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HouseholdMaintenanceItemResponse>> ListForHouseholdAsync(
        Guid householdId,
        IReadOnlyCollection<MaintenanceItemStatus>? statuses,
        Guid? assetId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceItemResponse> GetAsync(
        Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken = default);

    Task<MaintenanceItemResponse> PatchAsync(
        Guid assetId, Guid maintenanceItemId, PatchMaintenanceItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid assetId, Guid maintenanceItemId, CancellationToken cancellationToken = default);

    Task<ChecklistItemResponse> CreateChecklistItemAsync(
        Guid assetId, Guid maintenanceItemId, CreateChecklistItemRequest request, CancellationToken cancellationToken = default);

    Task<ChecklistItemResponse> PatchChecklistItemAsync(
        Guid assetId,
        Guid maintenanceItemId,
        Guid checklistItemId,
        PatchChecklistItemRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteChecklistItemAsync(
        Guid assetId, Guid maintenanceItemId, Guid checklistItemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChecklistItemResponse>> ReorderChecklistItemsAsync(
        Guid assetId, Guid maintenanceItemId, ReorderChecklistItemsRequest request, CancellationToken cancellationToken = default);

    Task<PartLineResponse> CreatePartLineAsync(
        Guid assetId, Guid maintenanceItemId, CreatePartLineRequest request, CancellationToken cancellationToken = default);

    Task<PartLineResponse> PatchPartLineAsync(
        Guid assetId, Guid maintenanceItemId, Guid partLineId, PatchPartLineRequest request, CancellationToken cancellationToken = default);

    Task DeletePartLineAsync(
        Guid assetId, Guid maintenanceItemId, Guid partLineId, CancellationToken cancellationToken = default);
}
