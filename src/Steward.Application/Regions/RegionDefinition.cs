namespace Steward.Application.Regions;

public record RegionDefinition(string Code, string Name);

public record CountryDefinition(string Code, string Name, IReadOnlyList<RegionDefinition> Regions);
