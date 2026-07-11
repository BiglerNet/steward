using System.Text.RegularExpressions;

namespace Steward.Application.VinDecode;

public static partial class VinFormat
{
    public static bool IsValid(string? vin) => vin is not null && VinPattern().IsMatch(vin);

    [GeneratedRegex("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.IgnoreCase)]
    private static partial Regex VinPattern();
}
