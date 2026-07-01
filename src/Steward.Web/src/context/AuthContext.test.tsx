import { act, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider, useAuth } from "@/context/AuthContext";
import { readSession } from "@/lib/session";

vi.mock("@/api/auth");

const authResponse = {
  token: "token-123",
  expiresAt: "2026-01-01T00:00:00Z",
  user: { id: "user-1", email: "user@example.com", displayName: "User One" },
  pendingInvites: [{ inviteCode: "abc", householdName: "Smith Garage", role: "Contributor" as const, expiresAt: "2026-02-01T00:00:00Z" }],
};

function Probe() {
  const { user, isAuthenticated, login, register, logout } = useAuth();
  return (
    <div>
      <span data-testid="status">{isAuthenticated ? "in" : "out"}</span>
      <span data-testid="user">{user?.email ?? "none"}</span>
      <button onClick={() => login({ email: "user@example.com", password: "pw" })}>login</button>
      <button
        onClick={() =>
          register({ email: "user@example.com", password: "pw", displayName: "User One" })
        }
      >
        register
      </button>
      <button onClick={logout}>logout</button>
    </div>
  );
}

describe("AuthContext", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("logs in and persists the session", async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse);

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await act(async () => {
      screen.getByText("login").click();
    });

    expect(screen.getByTestId("status").textContent).toBe("in");
    expect(screen.getByTestId("user").textContent).toBe("user@example.com");
    expect(readSession()?.token).toBe("token-123");
  });

  it("registers and persists the session", async () => {
    vi.mocked(authApi.register).mockResolvedValue(authResponse);

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await act(async () => {
      screen.getByText("register").click();
    });

    expect(screen.getByTestId("status").textContent).toBe("in");
    expect(readSession()?.user.email).toBe("user@example.com");
  });

  it("clears the session on logout", async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse);

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await act(async () => {
      screen.getByText("login").click();
    });
    await act(async () => {
      screen.getByText("logout").click();
    });

    expect(screen.getByTestId("status").textContent).toBe("out");
    expect(readSession()).toBeNull();
  });

  it("restores the session from localStorage on mount", () => {
    localStorage.setItem(
      "mt.session",
      JSON.stringify({
        token: authResponse.token,
        expiresAt: authResponse.expiresAt,
        user: authResponse.user,
        pendingInvites: authResponse.pendingInvites,
      })
    );

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    expect(screen.getByTestId("status").textContent).toBe("in");
    expect(screen.getByTestId("user").textContent).toBe("user@example.com");
  });
});
