import { apiClient } from "@/api/client";
import type {
  CreateDashboardRequest,
  DashboardDetailResponse,
  DashboardSnapshot,
  DashboardSummaryResponse,
  ReplaceWidgetLayoutRequest,
  UpdateDashboardRequest,
} from "@/api/types";

export async function listDashboards(householdId: string): Promise<DashboardSummaryResponse[]> {
  const { data } = await apiClient.get<DashboardSummaryResponse[]>(
    `/api/households/${householdId}/dashboards`
  );
  return data;
}

export async function getDashboard(
  householdId: string,
  dashboardId: string
): Promise<DashboardDetailResponse> {
  const { data } = await apiClient.get<DashboardDetailResponse>(
    `/api/households/${householdId}/dashboards/${dashboardId}`
  );
  return data;
}

export async function createDashboard(
  householdId: string,
  request: CreateDashboardRequest
): Promise<DashboardSummaryResponse> {
  const { data } = await apiClient.post<DashboardSummaryResponse>(
    `/api/households/${householdId}/dashboards`,
    request
  );
  return data;
}

export async function updateDashboard(
  householdId: string,
  dashboardId: string,
  request: UpdateDashboardRequest
): Promise<DashboardSummaryResponse> {
  const { data } = await apiClient.put<DashboardSummaryResponse>(
    `/api/households/${householdId}/dashboards/${dashboardId}`,
    request
  );
  return data;
}

export async function deleteDashboard(householdId: string, dashboardId: string): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/dashboards/${dashboardId}`);
}

export async function replaceWidgetLayout(
  householdId: string,
  dashboardId: string,
  request: ReplaceWidgetLayoutRequest
): Promise<DashboardDetailResponse> {
  const { data } = await apiClient.put<DashboardDetailResponse>(
    `/api/households/${householdId}/dashboards/${dashboardId}/widgets`,
    request
  );
  return data;
}

export async function getDashboardSnapshot(
  householdId: string,
  dashboardId: string
): Promise<DashboardSnapshot> {
  const { data } = await apiClient.get<DashboardSnapshot>(
    `/api/households/${householdId}/dashboards/${dashboardId}/snapshot`
  );
  return data;
}
