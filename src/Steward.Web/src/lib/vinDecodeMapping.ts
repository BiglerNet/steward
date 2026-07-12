import type { AssetCategory, FuelType } from "@/api/types";

const VIN_PATTERN = /^[A-HJ-NPR-Z0-9]{17}$/i;

export function isValidVinFormat(vin: string): boolean {
  return VIN_PATTERN.test(vin);
}

const FUEL_TYPE_LOOKUP: Record<string, FuelType> = {
  gasoline: "Gasoline",
  diesel: "Diesel",
};

/// vPIC's FuelTypePrimary is a free-text string; anything not recognized is left unset
/// rather than guessed at, since prefill is an accelerator, not a source of truth.
export function mapFuelTypePrimary(value: string | null): FuelType | null {
  if (!value) {
    return null;
  }
  return FUEL_TYPE_LOOKUP[value.trim().toLowerCase()] ?? null;
}

/// Coarse, intentionally forgiving keyword map used only to surface a soft hint —
/// never to block or clear input.
const CATEGORY_KEYWORDS: Partial<Record<AssetCategory, string[]>> = {
  Car: ["sedan", "coupe", "hatchback", "wagon", "convertible", "passenger car"],
  Truck: ["truck", "pickup"],
  Suv: ["suv", "sport utility", "utility vehicle"],
  Van: ["van", "minivan"],
  Motorcycle: ["motorcycle"],
  Utv: ["utv", "side by side", "side-by-side", "off-road"],
  Atv: ["atv", "all-terrain"],
  Snowmobile: ["snowmobile"],
  DirtBike: ["motorcycle", "dirt bike", "off-road"],
};

export function bodyClassMismatchHint(
  category: AssetCategory,
  bodyClass: string | null,
  vehicleType: string | null
): string | null {
  const keywords = CATEGORY_KEYWORDS[category];
  if (!keywords) {
    return null;
  }

  const decodedText = [bodyClass, vehicleType].filter(Boolean).join(" ").toLowerCase();
  if (!decodedText) {
    return null;
  }

  const matches = keywords.some((keyword) => decodedText.includes(keyword));
  if (matches) {
    return null;
  }

  const label = bodyClass ?? vehicleType;
  return `Decoded as ${label} — double-check the asset type.`;
}
