import type { CountryDefinition } from "@/api/types";

/// Trimmed registry for component tests; mirrors the shape served by GET /api/regions.
export const testRegionRegistry: CountryDefinition[] = [
  {
    code: "US",
    name: "United States",
    regions: [
      { code: "US-WI", name: "Wisconsin" },
      { code: "US-MN", name: "Minnesota" },
      { code: "US-CA", name: "California" },
    ],
  },
  {
    code: "CA",
    name: "Canada",
    regions: [
      { code: "CA-ON", name: "Ontario" },
      { code: "CA-BC", name: "British Columbia" },
    ],
  },
];
