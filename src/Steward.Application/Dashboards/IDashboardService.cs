namespace Steward.Application.Dashboards;

public interface IDashboardService
{
    Task<IReadOnlyList<DashboardSummaryResponse>> ListAsync(
        Guid householdId, CancellationToken cancellationToken = default);

    Task<DashboardDetailResponse> GetAsync(
        Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default);

    Task<DashboardSummaryResponse> CreateAsync(
        Guid householdId, CreateDashboardRequest request, CancellationToken cancellationToken = default);

    Task<DashboardSummaryResponse> UpdateAsync(
        Guid householdId, Guid dashboardId, UpdateDashboardRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default);

    Task<DashboardDetailResponse> ReplaceWidgetLayoutAsync(
        Guid householdId, Guid dashboardId, ReplaceWidgetLayoutRequest request, CancellationToken cancellationToken = default);

    Task<Dictionary<string, object>> GetSnapshotAsync(
        Guid householdId, Guid dashboardId, CancellationToken cancellationToken = default);
}
