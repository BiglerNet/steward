using Steward.Domain.Enums;
using DriveType = Steward.Domain.Enums.DriveType;

namespace Steward.Domain.Entities.Assets;

/// Boats identify by HIN (Hull Identification Number), which does not share VIN semantics,
/// so Boat is a sibling of Vehicle rather than a subclass. Shared by PowerBoat and Sailboat
/// categories; HullType/DriveType/KeelType/MastHeightFt/MastCount are registry-gated per category.
public class Boat : Asset
{
    public string? Hin { get; set; }
    public string? HullMaterial { get; set; }
    public HullType? HullType { get; set; }
    public DriveType? DriveType { get; set; }
    public string? KeelType { get; set; }
    public decimal? MastHeightFt { get; set; }
    public int? MastCount { get; set; }
    public decimal? LengthFt { get; set; }
    public decimal? BeamFt { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
}
