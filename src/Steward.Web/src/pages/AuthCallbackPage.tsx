import { useEffect, useRef } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { toast } from "sonner";
import { useAuth } from "@/context/AuthContext";

export function AuthCallbackPage() {
  const { exchangeOAuthCode } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const hasRun = useRef(false);

  useEffect(() => {
    if (hasRun.current) {
      return;
    }
    hasRun.current = true;

    const code = searchParams.get("code");
    if (!code) {
      toast.error("Missing OAuth code.");
      navigate("/login", { replace: true });
      return;
    }

    exchangeOAuthCode({ code })
      .then(() => navigate("/", { replace: true }))
      .catch(() => {
        toast.error("Couldn't complete sign-in. Please try again.");
        navigate("/login", { replace: true });
      });
  }, [searchParams, exchangeOAuthCode, navigate]);

  return (
    <div className="flex min-h-svh items-center justify-center">
      <p className="text-sm text-muted-foreground">Completing sign-in…</p>
    </div>
  );
}
