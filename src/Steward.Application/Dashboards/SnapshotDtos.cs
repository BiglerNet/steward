namespace Steward.Application.Dashboards;

public record DashboardSnapshotResponse(Dictionary<string, object> Data);

public record AssetCountData(int Count);

public record CylinderIndexData(int TotalCylinders, int EngineCount);

public record TotalDisplacementData(decimal TotalCc, int EngineCount);

public record TotalHorsepowerData(decimal TotalHp, int EngineCount);

public record TotalTorqueData(decimal TotalNm, int EngineCount);

public record DueSoonData(IReadOnlyList<DueItem> Items);

public record DueItem(
    Guid AssetId,
    string AssetName,
    string RecordType,
    DateOnly? ExpiresOn,
    string Urgency,
    string? StepText = null,
    string? EngineLabel = null);

public record RecentActivityData(IReadOnlyList<ActivityItem> Items);

public record ActivityItem(
    Guid AssetId,
    string AssetName,
    string Description,
    DateOnly PerformedOn,
    decimal? Cost);

public record FuelCostYtdData(decimal TotalCost, int LogCount);

public record MileageMtdData(decimal TotalMiles, int LogCount);
