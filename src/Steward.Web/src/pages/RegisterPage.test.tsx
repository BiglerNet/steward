import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as authApi from "@/api/auth";
import { AuthProvider } from "@/context/AuthContext";
import { RegisterPage } from "@/pages/RegisterPage";

vi.mock("@/api/auth");

function renderRegisterPage() {
  return render(
    <MemoryRouter initialEntries={["/register"]}>
      <AuthProvider>
        <RegisterPage />
      </AuthProvider>
    </MemoryRouter>
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
      expiresAt: "2026-01-01T00:00:00Z",
      user: { id: "1", email: "new@example.com", displayName: "New User" },
      pendingInvites: [],
    });

    renderRegisterPage();
    const user = userEvent.setup();

    await user.type(screen.getByLabelText("Display name"), "New User");
    await user.type(screen.getByLabelText("Email"), "new@example.com");
    await user.type(screen.getByLabelText("Password"), "password!23");
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
    await user.click(screen.getByRole("button", { name: "Create account" }));

    expect(await screen.findByText("Email is already in use.")).toBeInTheDocument();
    expect(screen.getByLabelText("Display name")).toHaveValue("New User");
  });
});
