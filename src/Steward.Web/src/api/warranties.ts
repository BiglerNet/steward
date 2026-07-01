import { apiClient } from "@/api/client";
import type { CreateWarrantyRequest, UpdateWarrantyRequest, WarrantyResponse } from "@/api/types";

export async function listWarranties(
  householdId: string,
  assetId: string
): Promise<WarrantyResponse[]> {
  const { data } = await apiClient.get<WarrantyResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/warranties`
  );
  return data;
}

export async function createWarranty(
  householdId: string,
  assetId: string,
  request: CreateWarrantyRequest
): Promise<WarrantyResponse> {
  const { data } = await apiClient.post<WarrantyResponse>(
    `/api/households/${householdId}/assets/${assetId}/warranties`,
    request
  );
  return data;
}

export async function updateWarranty(
  householdId: string,
  assetId: string,
  warrantyId: string,
  request: UpdateWarrantyRequest
): Promise<WarrantyResponse> {
  const { data } = await apiClient.put<WarrantyResponse>(
    `/api/households/${householdId}/assets/${assetId}/warranties/${warrantyId}`,
    request
  );
  return data;
}

export async function deleteWarranty(
  householdId: string,
  assetId: string,
  warrantyId: string
): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/assets/${assetId}/warranties/${warrantyId}`);
}
