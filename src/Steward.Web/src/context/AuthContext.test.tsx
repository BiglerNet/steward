import { act, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider, useAuth } from "@/context/AuthContext";
import { clearSession, readSession, writeSession, type StoredSession } from "@/lib/session";
import * as sessionRefreshModule from "@/lib/sessionRefresh";

vi.mock("@/api/auth");
vi.mock("@/lib/sessionRefresh");

const futureIso = new Date(Date.now() + 3_600_000).toISOString();

const authResponse = {
  token: "token-123",
  refreshToken: "refresh-token-123",
  expiresAt: futureIso,
  user: { id: "user-1", email: "user@example.com", displayName: "User One", themePreference: null },
  pendingInvites: [{ inviteCode: "abc", householdName: "Smith Garage", role: "Contributor" as const, expiresAt: "2026-02-01T00:00:00Z" }],
};

function Probe() {
  const { user, token, isAuthenticated, login, register, logout } = useAuth();
  return (
    <div>
      <span data-testid="status">{isAuthenticated ? "in" : "out"}</span>
      <span data-testid="user">{user?.email ?? "none"}</span>
      <span data-testid="token">{token ?? "none"}</span>
      <button onClick={() => login({ email: "user@example.com", password: "pw", rememberMe: true })}>login</button>
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

function seedSession(overrides: Partial<StoredSession> = {}): StoredSession {
  const session: StoredSession = {
    token: "token-123",
    refreshToken: "refresh-token-123",
    expiresAt: futureIso,
    user: authResponse.user,
    pendingInvites: [],
    ...overrides,
  };
  writeSession(session);
  return session;
}

describe("AuthContext", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.useRealTimers();
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
    expect(readSession()?.refreshToken).toBe("refresh-token-123");
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

  it("clears the session on logout and calls the logout endpoint", async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse);
    vi.mocked(authApi.logout).mockResolvedValue(undefined);

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
    expect(authApi.logout).toHaveBeenCalledWith({ refreshToken: "refresh-token-123" });
  });

  it("clears local state on logout even when the logout endpoint call fails", async () => {
    vi.mocked(authApi.login).mockResolvedValue(authResponse);
    vi.mocked(authApi.logout).mockRejectedValue(new Error("network error"));

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
    seedSession();

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    expect(screen.getByTestId("status").textContent).toBe("in");
    expect(screen.getByTestId("user").textContent).toBe("user@example.com");
  });

  it("proactively refreshes the session before the access token expires and reschedules", async () => {
    vi.useFakeTimers();
    const expiresAt = new Date(Date.now() + 65_000).toISOString();
    seedSession({ expiresAt });

    const rotated: StoredSession = {
      token: "token-456",
      refreshToken: "refresh-token-456",
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: authResponse.user,
      pendingInvites: [],
    };
    vi.mocked(sessionRefreshModule.refreshSession).mockImplementation(async () => {
      writeSession(rotated);
      return rotated;
    });

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    // Buffer is 60s, so the timer fires ~5s after the 65s-out expiry above.
    await act(async () => {
      await vi.advanceTimersByTimeAsync(6_000);
    });

    expect(sessionRefreshModule.refreshSession).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("token").textContent).toBe("token-456");
    expect(readSession()?.refreshToken).toBe("refresh-token-456");
  });

  it("clears the session when the proactive refresh fails", async () => {
    vi.useFakeTimers();
    const expiresAt = new Date(Date.now() + 65_000).toISOString();
    seedSession({ expiresAt });
    vi.mocked(sessionRefreshModule.refreshSession).mockImplementation(async () => {
      clearSession();
      return null;
    });

    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(6_000);
    });

    expect(screen.getByTestId("status").textContent).toBe("out");
    expect(readSession()).toBeNull();
  });

  it("adopts a session rotated by another tab via the storage event", () => {
    const initial = seedSession();
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );
    expect(screen.getByTestId("token").textContent).toBe("token-123");

    const rotated: StoredSession = { ...initial, token: "token-999", refreshToken: "refresh-token-999" };

    act(() => {
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: "mt.session",
          oldValue: JSON.stringify(initial),
          newValue: JSON.stringify(rotated),
        })
      );
    });

    expect(screen.getByTestId("token").textContent).toBe("token-999");
    expect(screen.getByTestId("status").textContent).toBe("in");
  });

  it("clears local state when another tab logs out", () => {
    const initial = seedSession();
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );
    expect(screen.getByTestId("status").textContent).toBe("in");

    act(() => {
      window.dispatchEvent(
        new StorageEvent("storage", {
          key: "mt.session",
          oldValue: JSON.stringify(initial),
          newValue: null,
        })
      );
    });

    expect(screen.getByTestId("status").textContent).toBe("out");
    expect(screen.getByTestId("user").textContent).toBe("none");
  });
});
