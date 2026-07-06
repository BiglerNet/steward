import { render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { AuthCallbackPage } from "@/pages/AuthCallbackPage";

vi.mock("@/api/auth");
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

function renderCallback(initialPath: string) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
          <Route path="/login" element={<div>login page</div>} />
          <Route path="/" element={<div>home page</div>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("AuthCallbackPage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("exchanges the code and navigates into the app on success", async () => {
    vi.mocked(authApi.exchangeOAuthCode).mockResolvedValue({
      token: "token-123",
      expiresAt: "2026-01-01T00:00:00Z",
      user: { id: "1", email: "user@example.com", displayName: null, themePreference: null },
      pendingInvites: [],
    });

    renderCallback("/auth/callback?code=abc123");

    await waitFor(() => expect(screen.getByText("home page")).toBeInTheDocument());
    expect(authApi.exchangeOAuthCode).toHaveBeenCalledWith({ code: "abc123" });
  });

  it("redirects to /login on a failed exchange", async () => {
    vi.mocked(authApi.exchangeOAuthCode).mockRejectedValue(new Error("expired code"));

    renderCallback("/auth/callback?code=expired");

    await waitFor(() => expect(screen.getByText("login page")).toBeInTheDocument());
  });
});
