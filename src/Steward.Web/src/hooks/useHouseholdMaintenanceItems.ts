import { useQuery } from "@tanstack/react-query";
import { listHouseholdMaintenanceItems, type HouseholdMaintenanceItemFilters } from "@/api/maintenanceItems";

export function useHouseholdMaintenanceItems(
  householdId: string,
  filters: HouseholdMaintenanceItemFilters = {}
) {
  return useQuery({
    queryKey: ["households", householdId, "maintenance-items", filters],
    queryFn: () => listHouseholdMaintenanceItems(householdId, filters),
  });
}
