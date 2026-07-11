import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import { RegisterPage } from "@/pages/RegisterPage";

vi.mock("@/api/auth");

function renderRegisterPage(
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
      <MemoryRouter initialEntries={["/register"]}>
        <AuthProvider>
          <ThemeProvider>
            <RegisterPage />
          </ThemeProvider>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("RegisterPage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("registers successfully", async () => {
    vi.mocked(authApi.register).mockResolvedValue({
      token: "token-123",
      refreshToken: "refresh-token-123",
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: { id: "1", email: "new@example.com", displayName: "New User", themePreference: null },
      pendingInvites: [],
    });

    renderRegisterPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Display name"), "New User");
    await user.type(screen.getByLabelText("Email"), "new@example.com");
    await user.type(screen.getByLabelText("Password"), "password!23");
    await user.type(screen.getByLabelText("Confirm password"), "password!23");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    await waitFor(() =>
      expect(authApi.register).toHaveBeenCalledWith({
        email: "new@example.com",
        password: "password!23",
        displayName: "New User",
      })
    );
  });

  it("renders inline field errors from a validation problem response", async () => {
    const error = Object.assign(new Error("Bad Request"), {
      isAxiosError: true,
      response: {
        status: 400,
        data: { errors: { Email: ["Email is already in use."] } },
      },
    });
    vi.mocked(authApi.register).mockRejectedValue(error);

    renderRegisterPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Display name"), "New User");
    await user.type(screen.getByLabelText("Email"), "taken@example.com");
    await user.type(screen.getByLabelText("Password"), "password!23");
    await user.type(screen.getByLabelText("Confirm password"), "password!23");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("Email is already in use.")).toBeInTheDocument();
    expect(screen.getByLabelText("Display name")).toHaveValue("New User");
  });

  it("blocks submission when the password confirmation does not match", async () => {
    renderRegisterPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Display name"), "New User");
    await user.type(screen.getByLabelText("Email"), "new@example.com");
    await user.type(screen.getByLabelText("Password"), "password!23");
    await user.type(screen.getByLabelText("Confirm password"), "different!23");
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("Passwords do not match")).toBeInTheDocument();
    expect(authApi.register).not.toHaveBeenCalled();
  });

  it("toggles password visibility without clearing the entered value", async () => {
    renderRegisterPage();
    const user = userEvent.setup();

    const passwordInput = screen.getByLabelText("Password");
    await user.type(passwordInput, "password!23");
    expect(passwordInput).toHaveAttribute("type", "password");

    await user.click(screen.getAllByRole("button", { name: "Show password" })[0]);

    expect(passwordInput).toHaveAttribute("type", "text");
    expect(passwordInput).toHaveValue("password!23");
  });

  it("updates the live password requirement indicators as the user types", async () => {
    renderRegisterPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Password"), "short");
    expect(screen.getByText("At least 8 characters")).toHaveClass("text-muted-foreground");
    expect(screen.getByText("At least one non-alphanumeric character")).toHaveClass("text-muted-foreground");

    await user.type(screen.getByLabelText("Password"), "er!23");
    expect(screen.getByText("At least 8 characters")).toHaveClass("text-emerald-600");
    expect(screen.getByText("At least one non-alphanumeric character")).toHaveClass("text-emerald-600");
  });

  it("renders no OAuth section or divider when no providers are configured", async () => {
    renderRegisterPage();

    await waitFor(() => expect(authApi.getOAuthProviders).toHaveBeenCalled());
    expect(screen.queryByText("or continue with")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /Continue with/ })).not.toBeInTheDocument();
  });
});
