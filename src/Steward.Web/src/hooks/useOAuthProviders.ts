import { useQuery } from "@tanstack/react-query";
import { getOAuthProviders } from "@/api/auth";

export function useOAuthProviders() {
  return useQuery({
    queryKey: ["oauth-providers"],
    queryFn: getOAuthProviders,
  });
}
