import { useQuery } from "@tanstack/react-query";
import { listAssetTypes } from "@/api/assetTypes";

/// The registry is static per deploy, so fetch it once per session and keep it cached.
export function useAssetTypeRegistry() {
  return useQuery({
    queryKey: ["asset-types"],
    queryFn: listAssetTypes,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}
