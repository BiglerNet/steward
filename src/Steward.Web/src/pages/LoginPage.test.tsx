import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import { LoginPage } from "@/pages/LoginPage";

vi.mock("@/api/auth");

function renderLoginPage(
  oauthProviders: { google: boolean; facebook: boolean; apple: boolean } = {
    google: false,
    facebook: false,
    apple: false,
  }
) {
  vi.mocked(authApi.getOAuthProviders).mockResolvedValue(oauthProviders);
  vi.mocked(authApi.oauthLoginUrl).mockImplementation(
    (provider, apiBaseUrl) => `${apiBaseUrl}/api/auth/oauth/${provider}/login`
  );
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/login"]}>
        <AuthProvider>
          <ThemeProvider>
            <LoginPage />
          </ThemeProvider>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("LoginPage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("logs in successfully", async () => {
    vi.mocked(authApi.login).mockResolvedValue({
      token: "token-123",
      expiresAt: "2026-01-01T00:00:00Z",
      user: { id: "1", email: "user@example.com", displayName: null, themePreference: null },
      pendingInvites: [],
    });

    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Email"), "user@example.com");
    await user.type(screen.getByLabelText("Password"), "password123");
    await user.click(screen.getByRole("button", { name: "Log in" }));

    await waitFor(() => expect(authApi.login).toHaveBeenCalledWith({
      email: "user@example.com",
      password: "password123",
    }));
  });

  it("shows an inline error on invalid credentials", async () => {
    const error = Object.assign(new Error("Unauthorized"), {
      isAxiosError: true,
      response: { status: 401, data: { title: "Invalid email or password." } },
    });
    vi.mocked(authApi.login).mockRejectedValue(error);

    renderLoginPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Email"), "user@example.com");
    await user.type(screen.getByLabelText("Password"), "wrong-password");
    await user.click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByText("Invalid email or password.")).toBeInTheDocument();
  });

  it("renders the OAuth section above the email/password form when a provider is configured", async () => {
    renderLoginPage({ google: true, facebook: false, apple: false });

    const googleButton = await screen.findByRole("link", { name: "Continue with Google" });
    const emailInput = screen.getByLabelText("Email");

    expect(
      googleButton.compareDocumentPosition(emailInput) & Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();
  });

  it("renders no OAuth section or divider when no providers are configured", async () => {
    renderLoginPage();

    await waitFor(() => expect(authApi.getOAuthProviders).toHaveBeenCalled());
    expect(screen.queryByText("or continue with")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /Continue with/ })).not.toBeInTheDocument();
  });
});
