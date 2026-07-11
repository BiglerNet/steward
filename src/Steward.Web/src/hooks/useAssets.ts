import { useQuery } from "@tanstack/react-query";
import { listAssets } from "@/api/assets";
import type { AssetCategory } from "@/api/types";

export function useAssets(householdId: string, category?: AssetCategory) {
  return useQuery({
    queryKey: ["households", householdId, "assets", { category }],
    queryFn: () => listAssets(householdId, category),
  });
}
