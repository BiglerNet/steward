import { z } from "zod";
import { optionalNumberString } from "@/lib/formHelpers";

export const engineFieldsSchema = z.object({
  label: z.string().min(1, "Label is required"),
  engineType: z.enum(["Ice", "Electric", "Hybrid"]),
  fuelType: z.enum(["Gasoline", "Diesel", "TwoStroke", "FourStroke", "Electric", "None"]),
  cylinders: optionalNumberString,
  displacementCc: optionalNumberString,
  horsepowerHp: optionalNumberString,
  torqueFtLbs: optionalNumberString,
});

export type EngineFieldsFormValues = z.infer<typeof engineFieldsSchema>;

export const defaultEngineFieldsValues: EngineFieldsFormValues = {
  label: "",
  engineType: "Ice",
  fuelType: "Gasoline",
  cylinders: "",
  displacementCc: "",
  horsepowerHp: "",
  torqueFtLbs: "",
};
