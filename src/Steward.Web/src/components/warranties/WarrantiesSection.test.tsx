import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as warrantiesApi from "@/api/warranties";
import type { WarrantyResponse } from "@/api/types";
import { WarrantiesSection } from "@/components/warranties/WarrantiesSection";
import * as useHouseholdsModule from "@/hooks/useHouseholds";

vi.mock("@/api/warranties");
vi.mock("@/hooks/useHouseholds");

const warranty: WarrantyResponse = {
  id: "warranty-1",
  assetId: "asset-1",
  provider: "Acme Corp",
  description: "Engine coverage",
  startsOn: "2025-01-01",
  expiresOn: "2025-12-31",
  notes: null,
  hasDocument: false,
  documentUrl: null,
};

function mockRole(userRole: "Owner" | "Contributor" | "Viewer") {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [
      {
        id: "house-1",
        name: "Garage",
        publicSlug: "garage",
        isPublicVisible: false,
        country: null,
        region: null,
        userRole,
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
}

function renderSection() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/warranties"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/warranties"
            element={<WarrantiesSection />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("WarrantiesSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(warrantiesApi.listWarranties).mockResolvedValue([warranty]);
  });

  it("creates a warranty via the add-entry form", async () => {
    mockRole("Contributor");
    vi.mocked(warrantiesApi.createWarranty).mockResolvedValue({
      ...warranty,
      id: "warranty-2",
      provider: "New Provider",
    });

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Acme Corp");
    await user.click(screen.getByRole("button", { name: "Add entry" }));
    await user.type(screen.getByLabelText("Provider"), "New Provider");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(warrantiesApi.createWarranty).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ provider: "New Provider" })
      )
    );
  });

  it("deletes a warranty for an Owner", async () => {
    mockRole("Owner");
    vi.mocked(warrantiesApi.deleteWarranty).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Acme Corp");
    await user.click(screen.getByRole("button", { name: "Delete" }));

    await waitFor(() =>
      expect(warrantiesApi.deleteWarranty).toHaveBeenCalledWith("house-1", "asset-1", "warranty-1")
    );
  });

  it("shows an expiry badge and hides controls for a Viewer", async () => {
    mockRole("Viewer");
    renderSection();

    await screen.findByText("Acme Corp");
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Add entry" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });

  it("edits the description through the WYSIWYG markdown editor", async () => {
    mockRole("Contributor");
    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Acme Corp");
    await user.click(screen.getByRole("button", { name: "Edit" }));

    const editor = await screen.findByLabelText("Description");
    expect(editor).toHaveAttribute("contenteditable", "true");
    expect(editor).toHaveTextContent("Engine coverage");
  });

  it("renders a markdown-formatted description as formatted markup in the list", async () => {
    vi.mocked(warrantiesApi.listWarranties).mockResolvedValue([
      { ...warranty, description: "**Full** coverage" },
    ]);
    mockRole("Viewer");
    renderSection();

    await screen.findByText("Acme Corp");
    expect(screen.getByText("Full").tagName.toLowerCase()).toBe("strong");
    expect(screen.queryByText("**Full** coverage")).not.toBeInTheDocument();
  });
});
