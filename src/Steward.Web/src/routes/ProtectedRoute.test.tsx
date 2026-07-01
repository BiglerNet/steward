import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "@/context/AuthContext";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { PublicOnlyRoute } from "@/routes/PublicOnlyRoute";

function renderApp(initialPath: string) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route element={<PublicOnlyRoute />}>
            <Route path="/login" element={<div>login page</div>} />
          </Route>
          <Route element={<ProtectedRoute />}>
            <Route path="/households/:id" element={<div>protected page</div>} />
          </Route>
        </Routes>
      </AuthProvider>
    </MemoryRouter>
  );
}

describe("route guards", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("redirects an unauthenticated visitor away from a protected route", () => {
    renderApp("/households/abc");

    expect(screen.getByText("login page")).toBeInTheDocument();
  });

  it("renders the protected route for an authenticated visitor", () => {
    localStorage.setItem(
      "mt.session",
      JSON.stringify({
        token: "token-123",
        expiresAt: "2026-01-01T00:00:00Z",
        user: { id: "1", email: "user@example.com", displayName: null },
        pendingInvites: [],
      })
    );

    renderApp("/households/abc");

    expect(screen.getByText("protected page")).toBeInTheDocument();
  });

  it("redirects an authenticated visitor away from a public-only route", () => {
    localStorage.setItem(
      "mt.session",
      JSON.stringify({
        token: "token-123",
        expiresAt: "2026-01-01T00:00:00Z",
        user: { id: "1", email: "user@example.com", displayName: null },
        pendingInvites: [],
      })
    );

    render(
      <MemoryRouter initialEntries={["/login"]}>
        <AuthProvider>
          <Routes>
            <Route element={<PublicOnlyRoute />}>
              <Route path="/login" element={<div>login page</div>} />
            </Route>
            <Route path="/" element={<div>home page</div>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    );

    expect(screen.getByText("home page")).toBeInTheDocument();
  });
});
