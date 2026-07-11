import type { CountryDefinition } from "@/api/types";

/// Region names ordered for issuing-authority suggestions: the household's own region
/// first, then the rest of its country, then other countries. Free typing is always
/// still allowed by the combobox that consumes this list.
export function issuingAuthoritySuggestions(
  registry: CountryDefinition[] | undefined,
  householdCountry: string | null | undefined,
  householdRegion: string | null | undefined
): string[] {
  if (!registry) {
    return [];
  }

  const home = registry.find((country) => country.code === householdCountry);
  const others = registry.filter((country) => country.code !== householdCountry);
  const names: string[] = [];

  if (home) {
    const homeRegion = home.regions.find((region) => region.code === householdRegion);
    if (homeRegion) {
      names.push(homeRegion.name);
    }
    for (const region of home.regions) {
      if (region.code !== householdRegion) {
        names.push(region.name);
      }
    }
  }

  for (const country of others) {
    for (const region of country.regions) {
      names.push(region.name);
    }
  }

  return names;
}
