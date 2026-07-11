import { apiClient } from "@/api/client";
import type { AssetTypeDefinition } from "@/api/types";

export async function listAssetTypes(): Promise<AssetTypeDefinition[]> {
  const { data } = await apiClient.get<AssetTypeDefinition[]>("/api/asset-types");
  return data;
}
