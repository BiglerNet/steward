import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { OAuthButtons, useOAuthSectionVisible } from "@/components/auth/OAuthButtons";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import * as useOAuthProvidersModule from "@/hooks/useOAuthProviders";
import type { OAuthProvidersResponse } from "@/api/types";

vi.mock("@/hooks/useOAuthProviders");

function mockProviders(data: OAuthProvidersResponse) {
  vi.mocked(useOAuthProvidersModule.useOAuthProviders).mockReturnValue({
    data,
  } as ReturnType<typeof useOAuthProvidersModule.useOAuthProviders>);
}

function renderButtons() {
  return render(
    <AuthProvider>
      <ThemeProvider>
        <OAuthButtons />
      </ThemeProvider>
    </AuthProvider>
  );
}

function VisibilityProbe() {
  const visible = useOAuthSectionVisible();
  return <span data-testid="visible">{String(visible)}</span>;
}

describe("OAuthButtons", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("renders only configured providers", () => {
    mockProviders({ google: true, facebook: true, apple: true });

    renderButtons();

    expect(screen.getByRole("link", { name: "Continue with Google" })).toBeInTheDocument();
  });

  it("collapses (renders nothing) when no providers are configured", () => {
    mockProviders({ google: false, facebook: false, apple: false });

    const { container } = renderButtons();

    expect(container).toBeEmptyDOMElement();
  });

  it("reports the section as not visible when no providers are configured", () => {
    mockProviders({ google: false, facebook: false, apple: false });

    render(
      <AuthProvider>
        <ThemeProvider>
          <VisibilityProbe />
        </ThemeProvider>
      </AuthProvider>
    );

    expect(screen.getByTestId("visible")).toHaveTextContent("false");
  });

  it("shows a pending/redirecting state on the clicked provider button", async () => {
    mockProviders({ google: true, facebook: false, apple: false });
    const user = userEvent.setup();

    renderButtons();

    const googleButton = screen.getByRole("link", { name: "Continue with Google" });
    await user.click(googleButton);

    expect(await screen.findByText("Redirecting…")).toBeInTheDocument();
  });
});
