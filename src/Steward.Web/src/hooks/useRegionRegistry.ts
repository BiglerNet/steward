import { useQuery } from "@tanstack/react-query";
import { listRegions } from "@/api/regions";

/// The registry is static per deploy, so fetch it once per session and keep it cached.
export function useRegionRegistry() {
  return useQuery({
    queryKey: ["regions"],
    queryFn: listRegions,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}
