import { useQuery } from "@tanstack/react-query";
import { listAssets } from "@/api/assets";
import type { AssetType } from "@/api/types";

export function useAssets(householdId: string, assetType?: AssetType) {
  return useQuery({
    queryKey: ["households", householdId, "assets", { assetType }],
    queryFn: () => listAssets(householdId, assetType),
  });
}
