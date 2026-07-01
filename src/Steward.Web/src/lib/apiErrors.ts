import axios from "axios";
import type { FieldValues, UseFormSetError } from "react-hook-form";
import type { ProblemDetailsResponse } from "@/api/types";

function toCamelCase(value: string): string {
  return value.charAt(0).toLowerCase() + value.slice(1);
}

export function applyValidationErrors<TFieldValues extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<TFieldValues>
): boolean {
  if (!axios.isAxiosError(error) || error.response?.status !== 400) {
    return false;
  }

  const problem = error.response.data as ProblemDetailsResponse | undefined;
  if (!problem?.errors) {
    return false;
  }

  for (const [field, messages] of Object.entries(problem.errors)) {
    setError(toCamelCase(field) as never, { message: messages[0] });
  }

  return true;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const problem = error.response?.data as ProblemDetailsResponse | undefined;
    if (problem?.title) {
      return problem.title;
    }
  }
  return fallback;
}
