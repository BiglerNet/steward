import { act, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider, useTheme } from "@/context/ThemeContext";

vi.mock("@/api/auth");

function createMatchMediaMock(initialMatches: boolean) {
  let matches = initialMatches;
  const listeners = new Set<(event: MediaQueryListEvent) => void>();

  const mql = {
    get matches() {
      return matches;
    },
    media: "(prefers-color-scheme: dark)",
    addEventListener: (_: string, listener: (event: MediaQueryListEvent) => void) => {
      listeners.add(listener);
    },
    removeEventListener: (_: string, listener: (event: MediaQueryListEvent) => void) => {
      listeners.delete(listener);
    },
  } as unknown as MediaQueryList;

  return {
    matchMedia: () => mql,
    fireChange: (next: boolean) => {
      matches = next;
      listeners.forEach((listener) => listener({ matches: next } as MediaQueryListEvent));
    },
  };
}

function Probe() {
  const { themePreference, resolvedTheme, setThemePreference } = useTheme();
  return (
    <div>
      <span data-testid="preference">{themePreference}</span>
      <span data-testid="resolved">{resolvedTheme}</span>
      <button onClick={() => setThemePreference("Dark")}>set-dark</button>
      <button onClick={() => setThemePreference("System")}>set-system</button>
    </div>
  );
}

describe("ThemeProvider", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("falls back to OS preference when nothing is stored anywhere", () => {
    const { matchMedia } = createMatchMediaMock(true);
    vi.stubGlobal("matchMedia", matchMedia);

    render(
      <AuthProvider>
        <ThemeProvider>
          <Probe />
        </ThemeProvider>
      </AuthProvider>
    );

    expect(screen.getByTestId("preference").textContent).toBe("System");
    expect(screen.getByTestId("resolved").textContent).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("uses a local override on this device when logged out", () => {
    const { matchMedia } = createMatchMediaMock(true); // OS says dark...
    vi.stubGlobal("matchMedia", matchMedia);
    localStorage.setItem("mt.themePreference", "Light"); // ...but the user explicitly chose light before logging in

    render(
      <AuthProvider>
        <ThemeProvider>
          <Probe />
        </ThemeProvider>
      </AuthProvider>
    );

    expect(screen.getByTestId("preference").textContent).toBe("Light");
    expect(screen.getByTestId("resolved").textContent).toBe("light");
  });

  it("prefers the authenticated user's stored preference over this device's local value", () => {
    const { matchMedia } = createMatchMediaMock(false);
    vi.stubGlobal("matchMedia", matchMedia);
    localStorage.setItem("mt.themePreference", "Light");
    localStorage.setItem(
      "mt.session",
      JSON.stringify({
        token: "token-123",
        refreshToken: "refresh-token-123",
        expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        user: { id: "user-1", email: "user@example.com", displayName: null, themePreference: "Dark" },
        pendingInvites: [],
      })
    );

    render(
      <AuthProvider>
        <ThemeProvider>
          <Probe />
        </ThemeProvider>
      </AuthProvider>
    );

    expect(screen.getByTestId("preference").textContent).toBe("Dark");
    expect(screen.getByTestId("resolved").textContent).toBe("dark");
  });

  it("tracks OS preference changes live while resolved to System", () => {
    const { matchMedia, fireChange } = createMatchMediaMock(false);
    vi.stubGlobal("matchMedia", matchMedia);

    render(
      <AuthProvider>
        <ThemeProvider>
          <Probe />
        </ThemeProvider>
      </AuthProvider>
    );

    expect(screen.getByTestId("resolved").textContent).toBe("light");

    act(() => {
      fireChange(true);
    });

    expect(screen.getByTestId("resolved").textContent).toBe("dark");
  });

  it("writes to localStorage and calls the API when authenticated on change", async () => {
    const { matchMedia } = createMatchMediaMock(false);
    vi.stubGlobal("matchMedia", matchMedia);
    vi.mocked(authApi.updateThemePreference).mockResolvedValue({
      id: "user-1",
      email: "user@example.com",
      displayName: null,
      avatarUrl: null,
      themePreference: "Dark",
    });
    localStorage.setItem(
      "mt.session",
      JSON.stringify({
        token: "token-123",
        refreshToken: "refresh-token-123",
        expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
        user: { id: "user-1", email: "user@example.com", displayName: null, themePreference: "Light" },
        pendingInvites: [],
      })
    );

    render(
      <AuthProvider>
        <ThemeProvider>
          <Probe />
        </ThemeProvider>
      </AuthProvider>
    );

    await act(async () => {
      screen.getByText("set-dark").click();
    });

    expect(authApi.updateThemePreference).toHaveBeenCalledWith({ themePreference: "Dark" });
    expect(localStorage.getItem("mt.themePreference")).toBe("Dark");
  });
});
