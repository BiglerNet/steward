using Microsoft.AspNetCore.Identity;
using Steward.Domain.Enums;

namespace Steward.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public ThemePreference? ThemePreference { get; set; }
}
