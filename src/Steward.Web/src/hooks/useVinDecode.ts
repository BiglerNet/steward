import { useMutation } from "@tanstack/react-query";
import { decodeVin } from "@/api/vinDecode";

export function useVinDecode() {
  return useMutation({
    mutationFn: (vin: string) => decodeVin(vin),
  });
}
