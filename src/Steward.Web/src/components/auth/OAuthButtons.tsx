import { Loader2 } from "lucide-react";
import { useState } from "react";
import { oauthLoginUrl } from "@/api/auth";
import type { OAuthProvidersResponse } from "@/api/types";
import googleDark from "@/assets/oauth/google-dark.svg";
import googleLight from "@/assets/oauth/google-light.svg";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/context/ThemeContext";
import { useOAuthProviders } from "@/hooks/useOAuthProviders";
import { cn } from "@/lib/utils";

// Facebook/Apple are configurable on the backend but have no brand icon assets
// yet (src/assets/oauth) — add entries here once those SVGs are supplied.
interface ProviderDef {
  id: keyof OAuthProvidersResponse;
  label: string;
  iconLight: string;
  iconDark: string;
}

const PROVIDERS: ProviderDef[] = [
  { id: "google", label: "Continue with Google", iconLight: googleLight, iconDark: googleDark },
];

function useEnabledProviders(): ProviderDef[] {
  const { data } = useOAuthProviders();
  return PROVIDERS.filter((provider) => data?.[provider.id]);
}

// eslint-disable-next-line react-refresh/only-export-components
export function useOAuthSectionVisible(): boolean {
  return useEnabledProviders().length > 0;
}

export function OAuthButtons() {
  const enabledProviders = useEnabledProviders();
  const { resolvedTheme } = useTheme();
  const [pendingProvider, setPendingProvider] = useState<string | null>(null);
  const apiBaseUrl = window.__APP_CONFIG__?.apiBaseUrl ?? import.meta.env.VITE_API_BASE_URL;

  if (enabledProviders.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-col gap-2">
      {enabledProviders.map((provider) => {
        const isPending = pendingProvider === provider.id;
        const isDisabled = pendingProvider !== null && !isPending;
        const icon = resolvedTheme === "dark" ? provider.iconDark : provider.iconLight;

        return (
          <Button
            key={provider.id}
            variant="outline"
            asChild
            className={cn(isDisabled && "pointer-events-none opacity-50")}
          >
            <a
              href={oauthLoginUrl(provider.id, apiBaseUrl)}
              aria-disabled={isDisabled}
              onClick={(event) => {
                if (isDisabled) {
                  event.preventDefault();
                  return;
                }
                setPendingProvider(provider.id);
              }}
            >
              {isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
              ) : (
                <img src={icon} alt="" className="h-4 w-4" aria-hidden />
              )}
              {isPending ? "Redirecting…" : provider.label}
            </a>
          </Button>
        );
      })}
    </div>
  );
}
