import { apiClient } from "@/api/client";
import type { MaintenanceScheduleEntryResponse } from "@/api/types";

export async function getMaintenanceSchedule(
  householdId: string,
  assetId: string
): Promise<MaintenanceScheduleEntryResponse[]> {
  const { data } = await apiClient.get<MaintenanceScheduleEntryResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/maintenance-schedule`
  );
  return data;
}
