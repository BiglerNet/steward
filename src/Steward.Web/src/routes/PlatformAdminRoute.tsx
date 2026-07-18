import { Navigate, Outlet } from "react-router";
import { useAuth } from "@/context/AuthContext";
import { useIsPlatformAdmin } from "@/hooks/useIsPlatformAdmin";

export function PlatformAdminRoute() {
  const { isAuthenticated } = useAuth();
  const isPlatformAdmin = useIsPlatformAdmin();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!isPlatformAdmin) {
    return <Navigate to="/households" replace />;
  }

  return <Outlet />;
}
