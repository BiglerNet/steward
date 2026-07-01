import { useQuery } from "@tanstack/react-query";
import { listHouseholds } from "@/api/households";

export function useHouseholds() {
  return useQuery({
    queryKey: ["households"],
    queryFn: listHouseholds,
  });
}
