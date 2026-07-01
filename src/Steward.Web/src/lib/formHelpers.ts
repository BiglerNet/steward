import { z } from "zod";

export const optionalNumberString = z
  .string()
  .optional()
  .refine((value) => !value || !Number.isNaN(Number(value)), "Enter a valid number");

export function parseOptionalNumber(value: string | undefined): number | null {
  return value === undefined || value === "" ? null : Number(value);
}

export function parseOptionalText(value: string | undefined): string | null {
  return value === undefined || value === "" ? null : value;
}

export function numberToInputValue(value: number | null | undefined): string {
  return value === null || value === undefined ? "" : String(value);
}

export function textToInputValue(value: string | null | undefined): string {
  return value ?? "";
}
