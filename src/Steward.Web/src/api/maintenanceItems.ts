import { apiClient } from "@/api/client";
import type {
  ChecklistItemResponse,
  CreateChecklistItemRequest,
  CreateMaintenanceItemRequest,
  CreatePartLineRequest,
  HouseholdMaintenanceItemResponse,
  MaintenanceItemResponse,
  MaintenanceItemStatus,
  PartLineResponse,
  PatchChecklistItemRequest,
  PatchMaintenanceItemRequest,
  PatchPartLineRequest,
} from "@/api/types";

export interface HouseholdMaintenanceItemFilters {
  status?: MaintenanceItemStatus[];
  assetId?: string;
}

export async function listHouseholdMaintenanceItems(
  householdId: string,
  filters: HouseholdMaintenanceItemFilters = {}
): Promise<HouseholdMaintenanceItemResponse[]> {
  const params = new URLSearchParams();
  filters.status?.forEach((status) => params.append("status", status));
  if (filters.assetId) params.append("assetId", filters.assetId);

  const { data } = await apiClient.get<HouseholdMaintenanceItemResponse[]>(
    `/api/households/${householdId}/maintenance-items`,
    { params }
  );
  return data;
}

export async function listMaintenanceItems(
  householdId: string,
  assetId: string
): Promise<MaintenanceItemResponse[]> {
  const { data } = await apiClient.get<MaintenanceItemResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items`
  );
  return data;
}

export async function getMaintenanceItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string
): Promise<MaintenanceItemResponse> {
  const { data } = await apiClient.get<MaintenanceItemResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}`
  );
  return data;
}

export async function createMaintenanceItem(
  householdId: string,
  assetId: string,
  request: CreateMaintenanceItemRequest
): Promise<MaintenanceItemResponse> {
  const { data } = await apiClient.post<MaintenanceItemResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items`,
    request
  );
  return data;
}

export async function patchMaintenanceItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  request: PatchMaintenanceItemRequest
): Promise<MaintenanceItemResponse> {
  const { data } = await apiClient.patch<MaintenanceItemResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}`,
    request
  );
  return data;
}

export async function deleteMaintenanceItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}`
  );
}

export async function createChecklistItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  request: CreateChecklistItemRequest
): Promise<ChecklistItemResponse> {
  const { data } = await apiClient.post<ChecklistItemResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/checklist-items`,
    request
  );
  return data;
}

export async function patchChecklistItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  checklistItemId: string,
  request: PatchChecklistItemRequest
): Promise<ChecklistItemResponse> {
  const { data } = await apiClient.patch<ChecklistItemResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/checklist-items/${checklistItemId}`,
    request
  );
  return data;
}

export async function deleteChecklistItem(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  checklistItemId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/checklist-items/${checklistItemId}`
  );
}

export async function reorderChecklistItems(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  checklistItemIds: string[]
): Promise<ChecklistItemResponse[]> {
  const { data } = await apiClient.put<ChecklistItemResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/checklist-items/reorder`,
    { checklistItemIds }
  );
  return data;
}

export async function createPartLine(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  request: CreatePartLineRequest
): Promise<PartLineResponse> {
  const { data } = await apiClient.post<PartLineResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/part-lines`,
    request
  );
  return data;
}

export async function patchPartLine(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  partLineId: string,
  request: PatchPartLineRequest
): Promise<PartLineResponse> {
  const { data } = await apiClient.patch<PartLineResponse>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/part-lines/${partLineId}`,
    request
  );
  return data;
}

export async function deletePartLine(
  householdId: string,
  assetId: string,
  maintenanceItemId: string,
  partLineId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/maintenance-items/${maintenanceItemId}/part-lines/${partLineId}`
  );
}
