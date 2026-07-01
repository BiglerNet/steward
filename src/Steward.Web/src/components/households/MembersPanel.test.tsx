import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as householdsApi from "@/api/households";
import { MembersPanel } from "@/components/households/MembersPanel";

vi.mock("@/api/households");
const toast = vi.hoisted(() => ({ error: vi.fn(), success: vi.fn() }));
vi.mock("sonner", () => ({ toast }));

const membersResponse = {
  members: [
    { userId: "u1", displayName: "Owner Person", email: "owner@example.com", role: "Owner" as const, status: "Active" as const },
    { userId: "u2", displayName: "Contributor Person", email: "contrib@example.com", role: "Contributor" as const, status: "Active" as const },
  ],
  pendingInvites: [
    { id: "inv-1", email: "pending@example.com", role: "Viewer" as const, inviteCode: "code-1", expiresAt: "2026-02-01T00:00:00Z", status: "Pending" as const },
  ],
};

function renderPanel(canManage: boolean) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MembersPanel householdId="house-1" canManage={canManage} />
    </QueryClientProvider>
  );
}

describe("MembersPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(householdsApi.listMembers).mockResolvedValue(membersResponse);
  });

  it("invites a member by email", async () => {
    vi.mocked(householdsApi.inviteMember).mockResolvedValue(undefined);

    renderPanel(true);
    const user = userEvent.setup();

    await screen.findByText("Owner Person");
    await user.type(screen.getByLabelText("Invite by email"), "new@example.com");
    await user.click(screen.getByRole("button", { name: "Send invite" }));

    await waitFor(() =>
      expect(householdsApi.inviteMember).toHaveBeenCalledWith("house-1", {
        email: "new@example.com",
        role: "Contributor",
      })
    );
  });

  it("revokes a pending invitation", async () => {
    vi.mocked(householdsApi.revokeInvitation).mockResolvedValue(undefined);

    renderPanel(true);
    const user = userEvent.setup();

    await screen.findByText("pending@example.com");
    await user.click(screen.getByRole("button", { name: "Revoke" }));

    await waitFor(() =>
      expect(householdsApi.revokeInvitation).toHaveBeenCalledWith("house-1", "code-1")
    );
  });

  it("removes a member", async () => {
    vi.mocked(householdsApi.removeMember).mockResolvedValue(undefined);

    renderPanel(true);
    const user = userEvent.setup();

    await screen.findByText("Contributor Person");
    await user.click(screen.getByRole("button", { name: "Remove" }));

    await waitFor(() => expect(householdsApi.removeMember).toHaveBeenCalledWith("house-1", "u2"));
  });

  it("shows a toast when removal is forbidden", async () => {
    const error = Object.assign(new Error("Forbidden"), {
      isAxiosError: true,
      response: { status: 403, data: { title: "Forbidden" } },
    });
    vi.mocked(householdsApi.removeMember).mockRejectedValue(error);

    renderPanel(true);
    const user = userEvent.setup();

    await screen.findByText("Contributor Person");
    await user.click(screen.getByRole("button", { name: "Remove" }));

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(screen.getByText("Contributor Person")).toBeInTheDocument();
  });

  it("hides management controls for a non-managing viewer", async () => {
    renderPanel(false);

    await screen.findByText("Owner Person");
    expect(screen.queryByRole("button", { name: "Remove" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Revoke" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Invite by email")).not.toBeInTheDocument();
  });
});
