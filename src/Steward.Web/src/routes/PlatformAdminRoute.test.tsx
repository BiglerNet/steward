import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "@/context/AuthContext";
import { PlatformAdminRoute } from "@/routes/PlatformAdminRoute";

function fakeJwt(claims: Record<string, unknown>): string {
  const base64url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
  return `${base64url({ alg: "none" })}.${base64url(claims)}.signature`;
}

function renderApp(initialPath: string) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<div>login page</div>} />
          <Route path="/households" element={<div>households page</div>} />
          <Route element={<PlatformAdminRoute />}>
            <Route path="/admin/templates" element={<div>admin templates page</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
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

describe("PlatformAdminRoute", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("redirects an unauthenticated visitor to login", () => {
    renderApp("/admin/templates");
    expect(screen.getByText("login page")).toBeInTheDocument();
  });

  it("redirects an authenticated non-admin away from admin routes", () => {
    seedSession(fakeJwt({ sub: "1", role: "Contributor" }));
    renderApp("/admin/templates");

    expect(screen.getByText("households page")).toBeInTheDocument();
    expect(screen.queryByText("admin templates page")).not.toBeInTheDocument();
  });

  it("renders admin content for a PlatformAdmin", () => {
    seedSession(fakeJwt({ sub: "1", role: "PlatformAdmin" }));
    renderApp("/admin/templates");

    expect(screen.getByText("admin templates page")).toBeInTheDocument();
  });

  it("renders admin content when role is one of several claims", () => {
    seedSession(fakeJwt({ sub: "1", role: ["Contributor", "PlatformAdmin"] }));
    renderApp("/admin/templates");

    expect(screen.getByText("admin templates page")).toBeInTheDocument();
  });
});
