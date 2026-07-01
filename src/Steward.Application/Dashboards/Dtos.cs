using Steward.Domain.Enums;

namespace Steward.Application.Dashboards;

public record DashboardSummaryResponse(Guid Id, string Name, bool IsDefault, int Position);

public record DashboardDetailResponse(
    Guid Id,
    string Name,
    bool IsDefault,
    int Position,
    IReadOnlyList<WidgetResponse> Widgets);

public record WidgetResponse(Guid Id, WidgetType WidgetType, WidgetSize WidgetSize, int Position, string? Config);

public record CreateDashboardRequest(string Name, bool? IsDefault);

public record UpdateDashboardRequest(string Name, bool IsDefault, int Position);

public record ReplaceWidgetLayoutRequest(IReadOnlyList<WidgetDefinition> Widgets);

public record WidgetDefinition(WidgetType WidgetType, WidgetSize WidgetSize, string? Config);
