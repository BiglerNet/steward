import { apiClient } from "@/api/client";
import type { AssetCategory, AssetResponse, CreateAssetRequest, UpdateAssetRequest } from "@/api/types";

export async function listAssets(
  householdId: string,
  category?: AssetCategory
): Promise<AssetResponse[]> {
  const { data } = await apiClient.get<AssetResponse[]>(`/api/households/${householdId}/assets`, {
    params: category ? { category } : undefined,
  });
  return data;
}

export async function getAsset(householdId: string, assetId: string): Promise<AssetResponse> {
  const { data } = await apiClient.get<AssetResponse>(
    `/api/households/${householdId}/assets/${assetId}`
  );
  return data;
}

export async function createAsset(
  householdId: string,
  request: CreateAssetRequest
): Promise<AssetResponse> {
  const { data } = await apiClient.post<AssetResponse>(
    `/api/households/${householdId}/assets`,
    request
  );
  return data;
}

export async function updateAsset(
  householdId: string,
  assetId: string,
  request: UpdateAssetRequest
): Promise<AssetResponse> {
  const { data } = await apiClient.put<AssetResponse>(
    `/api/households/${householdId}/assets/${assetId}`,
    request
  );
  return data;
}

export async function deleteAsset(householdId: string, assetId: string): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/assets/${assetId}`);
}
