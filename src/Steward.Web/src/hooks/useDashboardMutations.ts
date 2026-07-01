import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createDashboard,
  deleteDashboard,
  replaceWidgetLayout,
  updateDashboard,
} from "@/api/dashboards";
import type {
  CreateDashboardRequest,
  ReplaceWidgetLayoutRequest,
  UpdateDashboardRequest,
} from "@/api/types";

export function useCreateDashboard(householdId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateDashboardRequest) => createDashboard(householdId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "dashboards"] });
    },
  });
}

export function useUpdateDashboard(householdId: string, dashboardId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: UpdateDashboardRequest) =>
      updateDashboard(householdId, dashboardId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "dashboards"] });
    },
  });
}

export function useDeleteDashboard(householdId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (dashboardId: string) => deleteDashboard(householdId, dashboardId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "dashboards"] });
    },
  });
}

export function useReplaceWidgetLayout(householdId: string, dashboardId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: ReplaceWidgetLayoutRequest) =>
      replaceWidgetLayout(householdId, dashboardId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "dashboards"] });
      queryClient.invalidateQueries({
        queryKey: ["households", householdId, "dashboards", dashboardId, "snapshot"],
      });
    },
  });
}
