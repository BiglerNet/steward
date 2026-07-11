using Steward.Domain.Enums;

namespace Steward.Application.AssetTypes;

/// Structural class an asset category maps to; mirrors the concrete Domain entity classes.
public enum AssetStructuralType
{
    Vehicle,
    Boat,
    Trailer,
    Equipment,
}

public enum VinDecodeSupport
{
    None,
    BestEffort,
    Supported,
}

public record AssetTypeDefinition(
    AssetCategory Category,
    AssetGroup Group,
    AssetStructuralType StructuralType,
    string DisplayLabel,
    UsageTrackingMode DefaultUsageTrackingMode,
    bool TypicallyHasEngine,
    VinDecodeSupport VinDecodeSupport,
    IReadOnlyList<string> TypicalPermitKinds,
    IReadOnlyList<string> ApplicableFields,
    string Icon);
