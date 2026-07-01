import { z } from "zod";
import type { AssetFields, AssetType } from "@/api/types";
import { optionalNumberString, parseOptionalNumber, parseOptionalText } from "@/lib/formHelpers";

export type AssetTypeFieldKey =
  | "vin"
  | "color"
  | "make"
  | "model"
  | "hin"
  | "hullMaterial"
  | "lengthFt"
  | "beamFt"
  | "trackLengthIn"
  | "ballSizeIn"
  | "maxLoadLbs"
  | "interiorHeightFt"
  | "interiorLengthFt"
  | "cuttingWidthIn"
  | "maxPsi"
  | "maxGpm"
  | "equipmentDescription";

export interface AssetTypeField {
  key: AssetTypeFieldKey;
  label: string;
  kind: "text" | "number";
}

const TEXT_FIELDS = new Set<AssetTypeFieldKey>([
  "vin",
  "color",
  "make",
  "model",
  "hin",
  "hullMaterial",
  "equipmentDescription",
]);

function field(key: AssetTypeFieldKey, label: string): AssetTypeField {
  return { key, label, kind: TEXT_FIELDS.has(key) ? "text" : "number" };
}

const VEHICLE_FIELDS: AssetTypeField[] = [
  field("vin", "VIN"),
  field("color", "Color"),
  field("make", "Make"),
  field("model", "Model"),
];

export const ASSET_TYPE_LABELS: Record<AssetType, string> = {
  Snowmobile: "Snowmobile",
  Utv: "UTV",
  Boat: "Boat",
  Car: "Car",
  Truck: "Truck",
  SnowmobileTrailer: "Snowmobile Trailer",
  EnclosedTrailer: "Enclosed Trailer",
  RidingMower: "Riding Mower",
  PowerWasher: "Power Washer",
  SmallEngine: "Small Engine",
};

/** Per-type icon background colors, from docs/design/tokens.md `--light-asset-types`. */
export const assetTypeIconColors: Record<AssetType, string> = {
  Boat: "#dff6ff",
  Car: "#fff3e0",
  Truck: "#e8f5e9",
  Snowmobile: "#e3f2fd",
  Utv: "#fce4ec",
  SnowmobileTrailer: "#f3e5f5",
  EnclosedTrailer: "#f3e5f5",
  RidingMower: "#e8f5e9",
  PowerWasher: "#fff8e1",
  SmallEngine: "#f5f5dc",
};

export const assetTypeFieldConfig: Record<AssetType, AssetTypeField[]> = {
  Car: VEHICLE_FIELDS,
  Truck: VEHICLE_FIELDS,
  Utv: VEHICLE_FIELDS,
  Snowmobile: [...VEHICLE_FIELDS, field("trackLengthIn", "Track length (in)")],
  Boat: [
    field("color", "Color"),
    field("make", "Make"),
    field("model", "Model"),
    field("hin", "HIN"),
    field("hullMaterial", "Hull material"),
    field("lengthFt", "Length (ft)"),
    field("beamFt", "Beam (ft)"),
  ],
  SnowmobileTrailer: [field("ballSizeIn", "Ball size (in)"), field("maxLoadLbs", "Max load (lbs)")],
  EnclosedTrailer: [
    field("interiorHeightFt", "Interior height (ft)"),
    field("interiorLengthFt", "Interior length (ft)"),
  ],
  RidingMower: [field("cuttingWidthIn", "Cutting width (in)")],
  PowerWasher: [field("maxPsi", "Max PSI"), field("maxGpm", "Max GPM")],
  SmallEngine: [field("equipmentDescription", "Equipment description")],
};

export const ALL_ASSET_TYPE_FIELD_KEYS: AssetTypeFieldKey[] = [
  "vin",
  "color",
  "make",
  "model",
  "hin",
  "hullMaterial",
  "lengthFt",
  "beamFt",
  "trackLengthIn",
  "ballSizeIn",
  "maxLoadLbs",
  "interiorHeightFt",
  "interiorLengthFt",
  "cuttingWidthIn",
  "maxPsi",
  "maxGpm",
  "equipmentDescription",
];

function buildStringFieldSchemas(): Record<AssetTypeFieldKey, z.ZodTypeAny> {
  const schemas = {} as Record<AssetTypeFieldKey, z.ZodTypeAny>;
  for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
    schemas[key] = TEXT_FIELDS.has(key) ? z.string().optional() : optionalNumberString;
  }
  return schemas;
}

export const assetTypeFieldSchema = z.object(buildStringFieldSchemas());

export type AssetTypeFieldFormValues = Partial<Record<AssetTypeFieldKey, string>>;

export function clearInapplicableFields(
  assetType: AssetType,
  values: AssetTypeFieldFormValues
): Pick<AssetFields, AssetTypeFieldKey> {
  const applicableKeys = new Set(assetTypeFieldConfig[assetType].map((f) => f.key));
  const cleared: Record<string, string | number | null> = {};
  for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
    if (!applicableKeys.has(key)) {
      cleared[key] = null;
      continue;
    }
    cleared[key] = TEXT_FIELDS.has(key) ? parseOptionalText(values[key]) : parseOptionalNumber(values[key]);
  }
  return cleared as Pick<AssetFields, AssetTypeFieldKey>;
}
