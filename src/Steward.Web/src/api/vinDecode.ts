import { apiClient } from "@/api/client";
import type { VinDecodeResult } from "@/api/types";

export async function decodeVin(vin: string): Promise<VinDecodeResult> {
  const { data } = await apiClient.get<VinDecodeResult>(`/api/vin-decode/${vin}`);
  return data;
}
