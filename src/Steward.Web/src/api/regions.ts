import { apiClient } from "@/api/client";
import type { CountryDefinition } from "@/api/types";

export async function listRegions(): Promise<CountryDefinition[]> {
  const { data } = await apiClient.get<CountryDefinition[]>("/api/regions");
  return data;
}
