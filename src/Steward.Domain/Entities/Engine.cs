using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class Engine
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public required string Label { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public int? Year { get; set; }
    public EngineType EngineType { get; set; }
    public Mechanism? Mechanism { get; set; }
    public FuelType? FuelType { get; set; }
    public bool? IsExternallyChargeable { get; set; }
    public TwoStrokeOilDelivery? TwoStrokeOilDelivery { get; set; }
    public string? TwoStrokeMixRatio { get; set; }
    public int? Cylinders { get; set; }
    public decimal? DisplacementCC { get; set; }
    public EngineStatus Status { get; set; }
    public DateOnly? InstalledDate { get; set; }
    public decimal? InstalledAtAssetMiles { get; set; }
    public decimal? InstalledAtAssetHours { get; set; }
    public decimal? HorsepowerHp { get; set; }
    public decimal? TorqueNm { get; set; }
    public decimal? OilCapacityL { get; set; }
    public string? RecommendedOilType { get; set; }
    public decimal? CoolantCapacityL { get; set; }
    public int? RecommendedOctane { get; set; }
}
