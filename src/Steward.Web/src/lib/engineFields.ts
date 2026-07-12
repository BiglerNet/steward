import { z } from "zod";
import { optionalNumberString } from "@/lib/formHelpers";

export const WIZARD_ENGINE_TYPES = ["Ice", "Electric", "Hybrid", "Plug-in Hybrid"] as const;
export type WizardEngineType = (typeof WIZARD_ENGINE_TYPES)[number];

export const engineFieldsSchema = z.object({
  wizardEngineType: z.enum(["Ice", "Electric", "Hybrid", "Plug-in Hybrid"]),
  label: z.string().min(1, "Label is required"),
  mechanism: z.enum(["", "TwoStroke", "FourStroke", "Diesel", "Rotary"]).optional(),
  fuelType: z.enum(["", "Gasoline", "Diesel", "Propane"]).optional(),
  cylinders: optionalNumberString,
  displacementCc: optionalNumberString,
  horsepowerHp: optionalNumberString,
  torqueFtLbs: optionalNumberString,
  electricLabel: z.string().optional(),
  electricHorsepowerHp: optionalNumberString,
  electricTorqueFtLbs: optionalNumberString,
});

export type EngineFieldsFormValues = z.infer<typeof engineFieldsSchema>;

export const defaultEngineFieldsValues: EngineFieldsFormValues = {
  wizardEngineType: "Ice",
  label: "",
  mechanism: "",
  fuelType: "Gasoline",
  cylinders: "",
  displacementCc: "",
  horsepowerHp: "",
  torqueFtLbs: "",
  electricLabel: "",
  electricHorsepowerHp: "",
  electricTorqueFtLbs: "",
};

export function isHybridChoice(wizardEngineType: WizardEngineType): boolean {
  return wizardEngineType === "Hybrid" || wizardEngineType === "Plug-in Hybrid";
}
