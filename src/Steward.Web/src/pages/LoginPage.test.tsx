import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { LoginPage } from "@/pages/LoginPage";

vi.mock("@/api/auth");

function renderLoginPage() {
  return render(
    <MemoryRouter initialEntries={["/login"]}>
      <AuthProvider>
        <LoginPage />
      </AuthProvider>
    </MemoryRouter>
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
});
