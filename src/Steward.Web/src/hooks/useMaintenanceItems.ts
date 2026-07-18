import { useQuery } from "@tanstack/react-query";
import { getMaintenanceItem, listMaintenanceItems } from "@/api/maintenanceItems";

export function useMaintenanceItems(householdId: string, assetId: string) {
  return useQuery({
    queryKey: ["households", householdId, "assets", assetId, "maintenance-items"],
    queryFn: () => listMaintenanceItems(householdId, assetId),
  });
}

export function useMaintenanceItem(householdId: string, assetId: string, maintenanceItemId: string) {
  return useQuery({
    queryKey: ["households", householdId, "assets", assetId, "maintenance-items", maintenanceItemId],
    queryFn: () => getMaintenanceItem(householdId, assetId, maintenanceItemId),
  });
}
