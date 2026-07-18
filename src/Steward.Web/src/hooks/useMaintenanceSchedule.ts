import { useQuery } from "@tanstack/react-query";
import { getMaintenanceSchedule } from "@/api/maintenanceSchedule";

export function useMaintenanceSchedule(householdId: string, assetId: string) {
  return useQuery({
    queryKey: ["households", householdId, "assets", assetId, "maintenance-schedule"],
    queryFn: () => getMaintenanceSchedule(householdId, assetId),
  });
}
