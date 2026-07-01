import { apiClient } from "@/api/client";
import type { CreateEngineRequest, EngineResponse, UpdateEngineRequest } from "@/api/types";

export async function listEngines(householdId: string, assetId: string): Promise<EngineResponse[]> {
  const { data } = await apiClient.get<EngineResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/engines`
  );
  return data;
}

export async function createEngine(
  householdId: string,
  assetId: string,
  request: CreateEngineRequest
): Promise<EngineResponse> {
  const { data } = await apiClient.post<EngineResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines`,
    request
  );
  return data;
}

export async function updateEngine(
  householdId: string,
  assetId: string,
  engineId: string,
  request: UpdateEngineRequest
): Promise<EngineResponse> {
  const { data } = await apiClient.put<EngineResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}`,
    request
  );
  return data;
}

export async function retireEngine(
  householdId: string,
  assetId: string,
  engineId: string
): Promise<EngineResponse> {
  const { data } = await apiClient.post<EngineResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/retire`
  );
  return data;
}

export async function reactivateEngine(
  householdId: string,
  assetId: string,
  engineId: string
): Promise<EngineResponse> {
  const { data } = await apiClient.post<EngineResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/reactivate`
  );
  return data;
}

export async function markEngineBroken(
  householdId: string,
  assetId: string,
  engineId: string
): Promise<EngineResponse> {
  const { data } = await apiClient.post<EngineResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/mark-broken`
  );
  return data;
}

export async function deleteEngine(
  householdId: string,
  assetId: string,
  engineId: string
): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/assets/${assetId}/engines/${engineId}`);
}
