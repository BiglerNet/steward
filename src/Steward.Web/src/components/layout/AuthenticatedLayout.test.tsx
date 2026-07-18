import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";

vi.mock("@/hooks/useHouseholds");

function fakeJwt(claims: Record<string, unknown>): string {
  const base64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
  return `${base64url({ alg: "none" })}.${base64url(claims)}.signature`;
}

function seedSession(token: string) {
  localStorage.setItem(
    "mt.session",
    JSON.stringify({
      token,
      refreshToken: "refresh-token-123",
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: { id: "1", email: "user@example.com", displayName: null },
      pendingInvites: [],
    })
  );
}

function renderLayout() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households"]}>
        <AuthProvider>
          <ThemeProvider>
            <Routes>
              <Route element={<AuthenticatedLayout />}>
                <Route path="/households" element={<div>content</div>} />
              </Route>
            </Routes>
          </ThemeProvider>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AuthenticatedLayout — Admin nav link", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
      data: [],
    } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
  });

  it("hides the Admin link for a regular household member", () => {
    seedSession(fakeJwt({ sub: "1", role: "Contributor" }));
    renderLayout();

    expect(screen.queryByRole("link", { name: /admin/i })).not.toBeInTheDocument();
  });

  it("shows the Admin link for a PlatformAdmin, linking to /admin", () => {
    seedSession(fakeJwt({ sub: "1", role: "PlatformAdmin" }));
    renderLayout();

    const link = screen.getByRole("link", { name: /admin/i });
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute("href", "/admin");
  });
});
