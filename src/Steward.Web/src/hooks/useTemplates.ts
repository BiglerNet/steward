import { useQuery } from "@tanstack/react-query";
import { listHouseholdTemplates, listPlatformTemplates } from "@/api/templates";
import type { AssetCategory } from "@/api/types";

export function useHouseholdTemplates(householdId: string, assetCategory?: AssetCategory) {
  return useQuery({
    queryKey: ["households", householdId, "templates", { assetCategory }],
    queryFn: () => listHouseholdTemplates(householdId, assetCategory),
  });
}

export function usePlatformTemplates(assetCategory?: AssetCategory) {
  return useQuery({
    queryKey: ["templates", "platform", { assetCategory }],
    queryFn: () => listPlatformTemplates(assetCategory),
  });
}
