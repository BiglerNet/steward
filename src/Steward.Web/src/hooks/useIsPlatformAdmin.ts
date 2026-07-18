import { useAuth } from "@/context/AuthContext";
import { jwtHasRole } from "@/lib/jwt";

export function useIsPlatformAdmin(): boolean {
  const { token } = useAuth();
  return token ? jwtHasRole(token, "PlatformAdmin") : false;
}
