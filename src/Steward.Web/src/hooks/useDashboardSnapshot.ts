import { useQuery } from "@tanstack/react-query";
import { getDashboardSnapshot } from "@/api/dashboards";

export function useDashboardSnapshot(householdId: string, dashboardId: string | null) {
  return useQuery({
    queryKey: ["households", householdId, "dashboards", dashboardId, "snapshot"],
    queryFn: () => getDashboardSnapshot(householdId, dashboardId!),
    enabled: dashboardId != null,
    staleTime: 30_000,
  });
}
