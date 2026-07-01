import { useQuery } from "@tanstack/react-query";
import { getDashboard, listDashboards } from "@/api/dashboards";

export function useDashboards(householdId: string) {
  return useQuery({
    queryKey: ["households", householdId, "dashboards"],
    queryFn: () => listDashboards(householdId),
    staleTime: 60_000,
  });
}

export function useDashboard(householdId: string, dashboardId: string | null) {
  return useQuery({
    queryKey: ["households", householdId, "dashboards", dashboardId],
    queryFn: () => getDashboard(householdId, dashboardId!),
    enabled: dashboardId != null,
    staleTime: 60_000,
  });
}
