import { z } from "zod";
import type { AssetFields, AssetGroup, AssetResponse, AssetTypeDefinition } from "@/api/types";
import {
  numberToInputValue,
  optionalNumberString,
  parseOptionalNumber,
  parseOptionalText,
  textToInputValue,
} from "@/lib/formHelpers";

/// Which fields a category accepts comes from the served registry (`applicableFields`);
/// this module only keeps presentation concerns (labels, input kinds, group headings).

export type AssetTypeFieldKey =
  | "vin"
  | "make"
  | "model"
  | "color"
  | "trackLengthIn"
  | "hin"
  | "hullMaterial"
  | "hullType"
  | "driveType"
  | "keelType"
  | "mastHeightFt"
  | "mastCount"
  | "lengthFt"
  | "beamFt"
  | "ballSizeIn"
  | "maxLoadLbs"
  | "interiorHeightFt"
  | "interiorLengthFt"
  | "cuttingWidthIn"
  | "maxPsi"
  | "maxGpm"
  | "equipmentDescription"
  | "licensePlate";

export interface AssetTypeFieldOption {
  value: string;
  label: string;
}

export interface AssetTypeField {
  key: AssetTypeFieldKey;
  label: string;
  kind: "text" | "number" | "select";
  options?: readonly AssetTypeFieldOption[];
}

const HULL_TYPE_OPTIONS: readonly AssetTypeFieldOption[] = [
  { value: "Monohull", label: "Monohull" },
  { value: "Catamaran", label: "Catamaran" },
  { value: "Trimaran", label: "Trimaran" },
  { value: "Pontoon", label: "Pontoon" },
  { value: "Other", label: "Other" },
];

const DRIVE_TYPE_OPTIONS: readonly AssetTypeFieldOption[] = [
  { value: "Inboard", label: "Inboard" },
  { value: "Outboard", label: "Outboard" },
  { value: "SternDrive", label: "Stern drive (I/O)" },
  { value: "JetDrive", label: "Jet drive" },
];

const FIELD_PRESENTATION: Record<AssetTypeFieldKey, AssetTypeField> = {
  vin: { key: "vin", label: "VIN", kind: "text" },
  make: { key: "make", label: "Make", kind: "text" },
  model: { key: "model", label: "Model", kind: "text" },
  color: { key: "color", label: "Color", kind: "text" },
  trackLengthIn: { key: "trackLengthIn", label: "Track length (in)", kind: "number" },
  hin: { key: "hin", label: "HIN", kind: "text" },
  hullMaterial: { key: "hullMaterial", label: "Hull material", kind: "text" },
  hullType: { key: "hullType", label: "Hull type", kind: "select", options: HULL_TYPE_OPTIONS },
  driveType: { key: "driveType", label: "Drive type", kind: "select", options: DRIVE_TYPE_OPTIONS },
  keelType: { key: "keelType", label: "Keel type", kind: "text" },
  mastHeightFt: { key: "mastHeightFt", label: "Mast height (ft)", kind: "number" },
  mastCount: { key: "mastCount", label: "Mast count", kind: "number" },
  lengthFt: { key: "lengthFt", label: "Length (ft)", kind: "number" },
  beamFt: { key: "beamFt", label: "Beam (ft)", kind: "number" },
  ballSizeIn: { key: "ballSizeIn", label: "Ball size (in)", kind: "number" },
  maxLoadLbs: { key: "maxLoadLbs", label: "Max load (lbs)", kind: "number" },
  interiorHeightFt: { key: "interiorHeightFt", label: "Interior height (ft)", kind: "number" },
  interiorLengthFt: { key: "interiorLengthFt", label: "Interior length (ft)", kind: "number" },
  cuttingWidthIn: { key: "cuttingWidthIn", label: "Cutting width (in)", kind: "number" },
  maxPsi: { key: "maxPsi", label: "Max PSI", kind: "number" },
  maxGpm: { key: "maxGpm", label: "Max GPM", kind: "number" },
  equipmentDescription: { key: "equipmentDescription", label: "Equipment description", kind: "text" },
  licensePlate: { key: "licensePlate", label: "License plate", kind: "text" },
};

export const ALL_ASSET_TYPE_FIELD_KEYS = Object.keys(FIELD_PRESENTATION) as AssetTypeFieldKey[];

const TEXT_FIELDS = new Set<AssetTypeFieldKey>(
  ALL_ASSET_TYPE_FIELD_KEYS.filter((key) => FIELD_PRESENTATION[key].kind !== "number")
);

export function isTextField(key: AssetTypeFieldKey): boolean {
  return TEXT_FIELDS.has(key);
}

export const ASSET_GROUP_LABELS: Record<AssetGroup, string> = {
  Road: "Road",
  Powersport: "Powersports",
  Water: "Water",
  Trailer: "Trailers",
  Equipment: "Equipment",
};

export const ASSET_GROUP_ORDER: AssetGroup[] = ["Road", "Powersport", "Water", "Trailer", "Equipment"];

/// Fields to render for a category, in a stable order, from its registry entry.
export function fieldsFor(definition: AssetTypeDefinition): AssetTypeField[] {
  return ALL_ASSET_TYPE_FIELD_KEYS
    .filter((key) => definition.applicableFields.includes(key))
    .map((key) => FIELD_PRESENTATION[key]);
}

export function findDefinition(
  registry: AssetTypeDefinition[] | undefined,
  category: string | undefined
): AssetTypeDefinition | undefined {
  return registry?.find((d) => d.category === category);
}

/// Parses form values into the request's type-specific fields, nulling any field the
/// registry says is not applicable to the category (the backend rejects those).
export function clearInapplicableFields(
  definition: AssetTypeDefinition,
  values: Partial<Record<AssetTypeFieldKey, string>>
): Pick<AssetFields, AssetTypeFieldKey> {
  const cleared: Record<string, string | number | null> = {};
  for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
    if (!definition.applicableFields.includes(key)) {
      cleared[key] = null;
      continue;
    }
    cleared[key] = TEXT_FIELDS.has(key)
      ? parseOptionalText(values[key])
      : parseOptionalNumber(values[key]);
  }
  return cleared as Pick<AssetFields, AssetTypeFieldKey>;
}

function buildTypeFieldsShape() {
  const shape = {} as Record<AssetTypeFieldKey, z.ZodOptional<z.ZodString>>;
  for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
    shape[key] = z.string().optional();
  }
  return shape;
}

function emptyTypeFields(): Record<AssetTypeFieldKey, string> {
  return Object.fromEntries(ALL_ASSET_TYPE_FIELD_KEYS.map((key) => [key, ""])) as Record<
    AssetTypeFieldKey,
    string
  >;
}

/// Base asset fields plus the registry-driven type-specific fields, shared by the edit
/// dialog and the creation wizard's Details step. Category is intentionally excluded:
/// the dialog shows it as a disabled field and the wizard selects it in its own Type step.
export const assetFieldsSchema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
  year: optionalNumberString,
  usageTrackingMode: z.enum(["None", "Mileage", "Hours", "Both"]),
  ...buildTypeFieldsShape(),
});

export type AssetFieldsFormValues = z.infer<typeof assetFieldsSchema>;

export function defaultAssetFieldsValues(definition: AssetTypeDefinition): AssetFieldsFormValues {
  return {
    name: "",
    description: "",
    year: "",
    usageTrackingMode: definition.defaultUsageTrackingMode,
    ...emptyTypeFields(),
  };
}

export function assetToAssetFieldsValues(asset: AssetResponse): AssetFieldsFormValues {
  const typeFields = Object.fromEntries(
    ALL_ASSET_TYPE_FIELD_KEYS.map((key) => {
      const value = asset[key];
      return [key, typeof value === "number" ? numberToInputValue(value) : textToInputValue(value)];
    })
  );
  return {
    name: asset.name,
    description: textToInputValue(asset.description),
    year: numberToInputValue(asset.year),
    usageTrackingMode: asset.usageTrackingMode,
    ...typeFields,
  } as AssetFieldsFormValues;
}
