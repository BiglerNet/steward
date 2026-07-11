namespace Steward.Application.Regions;

/// Single source of truth for supported countries and their ISO 3166-2 regions.
/// Served to the frontend via GET /api/regions and used to validate Household.Country/Region.
public static class RegionRegistry
{
    private static readonly IReadOnlyList<RegionDefinition> UsStates =
    [
        new("US-AL", "Alabama"),
        new("US-AK", "Alaska"),
        new("US-AZ", "Arizona"),
        new("US-AR", "Arkansas"),
        new("US-CA", "California"),
        new("US-CO", "Colorado"),
        new("US-CT", "Connecticut"),
        new("US-DE", "Delaware"),
        new("US-FL", "Florida"),
        new("US-GA", "Georgia"),
        new("US-HI", "Hawaii"),
        new("US-ID", "Idaho"),
        new("US-IL", "Illinois"),
        new("US-IN", "Indiana"),
        new("US-IA", "Iowa"),
        new("US-KS", "Kansas"),
        new("US-KY", "Kentucky"),
        new("US-LA", "Louisiana"),
        new("US-ME", "Maine"),
        new("US-MD", "Maryland"),
        new("US-MA", "Massachusetts"),
        new("US-MI", "Michigan"),
        new("US-MN", "Minnesota"),
        new("US-MS", "Mississippi"),
        new("US-MO", "Missouri"),
        new("US-MT", "Montana"),
        new("US-NE", "Nebraska"),
        new("US-NV", "Nevada"),
        new("US-NH", "New Hampshire"),
        new("US-NJ", "New Jersey"),
        new("US-NM", "New Mexico"),
        new("US-NY", "New York"),
        new("US-NC", "North Carolina"),
        new("US-ND", "North Dakota"),
        new("US-OH", "Ohio"),
        new("US-OK", "Oklahoma"),
        new("US-OR", "Oregon"),
        new("US-PA", "Pennsylvania"),
        new("US-RI", "Rhode Island"),
        new("US-SC", "South Carolina"),
        new("US-SD", "South Dakota"),
        new("US-TN", "Tennessee"),
        new("US-TX", "Texas"),
        new("US-UT", "Utah"),
        new("US-VT", "Vermont"),
        new("US-VA", "Virginia"),
        new("US-WA", "Washington"),
        new("US-WV", "West Virginia"),
        new("US-WI", "Wisconsin"),
        new("US-WY", "Wyoming"),
        new("US-DC", "District of Columbia"),
    ];

    private static readonly IReadOnlyList<RegionDefinition> CaProvinces =
    [
        new("CA-AB", "Alberta"),
        new("CA-BC", "British Columbia"),
        new("CA-MB", "Manitoba"),
        new("CA-NB", "New Brunswick"),
        new("CA-NL", "Newfoundland and Labrador"),
        new("CA-NS", "Nova Scotia"),
        new("CA-NT", "Northwest Territories"),
        new("CA-NU", "Nunavut"),
        new("CA-ON", "Ontario"),
        new("CA-PE", "Prince Edward Island"),
        new("CA-QC", "Quebec"),
        new("CA-SK", "Saskatchewan"),
        new("CA-YT", "Yukon"),
    ];

    public static readonly IReadOnlyList<CountryDefinition> All =
    [
        new("US", "United States", UsStates),
        new("CA", "Canada", CaProvinces),
    ];

    private static readonly IReadOnlyDictionary<string, CountryDefinition> ByCode =
        All.ToDictionary(c => c.Code);

    public static bool IsValidCountry(string code) => ByCode.ContainsKey(code);

    public static bool IsValidRegion(string country, string region) =>
        ByCode.TryGetValue(country, out var definition) &&
        definition.Regions.Any(r => r.Code == region);
}
