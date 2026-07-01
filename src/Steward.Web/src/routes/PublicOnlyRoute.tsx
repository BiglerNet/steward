import { Navigate, Outlet } from "react-router";
import { useAuth } from "@/context/AuthContext";

export function PublicOnlyRoute() {
  const { isAuthenticated } = useAuth();

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}
