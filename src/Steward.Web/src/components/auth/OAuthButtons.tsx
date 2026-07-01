import { Button } from "@/components/ui/button";

const PROVIDERS: { id: string; label: string }[] = [
  { id: "google", label: "Continue with Google" },
  { id: "facebook", label: "Continue with Facebook" },
  { id: "apple", label: "Continue with Apple" },
];

export function OAuthButtons() {
  const apiBaseUrl = window.__APP_CONFIG__?.apiBaseUrl || import.meta.env.VITE_API_BASE_URL;

  return (
    <div className="flex flex-col gap-2">
      {PROVIDERS.map((provider) => (
        <Button key={provider.id} variant="outline" asChild>
          <a href={`${apiBaseUrl}/api/auth/oauth/${provider.id}/login`}>{provider.label}</a>
        </Button>
      ))}
    </div>
  );
}
