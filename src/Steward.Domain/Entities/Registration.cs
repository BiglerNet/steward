using Steward.Domain.Enums;

namespace Steward.Domain.Entities;

public class Registration
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public RegistrationKind Kind { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? RenewedOn { get; set; }
    public decimal? Cost { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Notes { get; set; }
}
