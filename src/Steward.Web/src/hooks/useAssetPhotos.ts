import { useQuery } from "@tanstack/react-query";
import { listAssetPhotos } from "@/api/assetPhotos";

export function useAssetPhotos(householdId: string, assetId: string) {
  return useQuery({
    queryKey: ["households", householdId, "assets", assetId, "photos"],
    queryFn: () => listAssetPhotos(householdId, assetId),
  });
}
