import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { UserMenu } from "@/components/auth/UserMenu";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";

vi.mock("@/api/auth");

function seedSession(themePreference: "Light" | "Dark" | "System" | null) {
  localStorage.setItem(
    "mt.session",
    JSON.stringify({
      token: "token-123",
      refreshToken: "refresh-token-123",
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: { id: "user-1", email: "user@example.com", displayName: "Test User", themePreference },
      pendingInvites: [],
    })
  );
}

function renderUserMenu() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <ThemeProvider>
          <UserMenu />
        </ThemeProvider>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("UserMenu theme control", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it("shows Light, Dark, and System options with the active one indicated", async () => {
    seedSession("Dark");
    renderUserMenu();
    const user = userEvent.setup();

    await user.click(screen.getByText("Test User"));

    const darkOption = await screen.findByRole("menuitemradio", { name: /Dark/i });
    const lightOption = screen.getByRole("menuitemradio", { name: /Light/i });
    const systemOption = screen.getByRole("menuitemradio", { name: /System/i });

    expect(darkOption).toHaveAttribute("aria-checked", "true");
    expect(lightOption).toHaveAttribute("aria-checked", "false");
    expect(systemOption).toHaveAttribute("aria-checked", "false");
  });

  it("selecting a theme calls the update endpoint", async () => {
    seedSession("Light");
    vi.mocked(authApi.updateThemePreference).mockResolvedValue({
      id: "user-1",
      email: "user@example.com",
      displayName: "Test User",
      avatarUrl: null,
      themePreference: "Dark",
    });
    renderUserMenu();
    const user = userEvent.setup();

    await user.click(screen.getByText("Test User"));
    await user.click(await screen.findByRole("menuitemradio", { name: /Dark/i }));

    expect(authApi.updateThemePreference).toHaveBeenCalledWith({ themePreference: "Dark" });
  });
});
