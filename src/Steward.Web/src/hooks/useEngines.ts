import { useQuery } from "@tanstack/react-query";
import { listEngines } from "@/api/engines";

export function useEngines(householdId: string, assetId: string) {
  return useQuery({
    queryKey: ["households", householdId, "assets", assetId, "engines"],
    queryFn: () => listEngines(householdId, assetId),
  });
}
