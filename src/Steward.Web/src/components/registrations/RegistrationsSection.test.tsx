import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as registrationsApi from "@/api/registrations";
import type { RegistrationResponse } from "@/api/types";
import { RegistrationsSection } from "@/components/registrations/RegistrationsSection";
import * as useHouseholdsModule from "@/hooks/useHouseholds";

vi.mock("@/api/registrations");
vi.mock("@/hooks/useHouseholds");

const registrationA: RegistrationResponse = {
  id: "reg-1",
  assetId: "asset-1",
  registrationNumber: "ABC-123",
  issuingAuthority: "DMV",
  renewedOn: "2025-01-01",
  cost: 50,
  expiresOn: "2025-12-31",
  notes: null,
  hasDocument: false,
  documentUrl: null,
};

const registrationB: RegistrationResponse = {
  id: "reg-2",
  assetId: "asset-1",
  registrationNumber: "XYZ-999",
  issuingAuthority: "DMV",
  renewedOn: "2026-01-01",
  cost: 60,
  expiresOn: "2026-12-31",
  notes: null,
  hasDocument: true,
  documentUrl: "/api/.../document",
};

function mockRole(userRole: "Owner" | "Contributor" | "Viewer") {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [
      {
        id: "house-1",
        name: "Garage",
        publicSlug: "garage",
        isPublicVisible: false,
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
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/registrations"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/registrations"
            element={<RegistrationsSection />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("RegistrationsSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(registrationsApi.listRegistrations).mockResolvedValue([registrationA, registrationB]);
  });

  it("lists registrations ordered by expiresOn descending", async () => {
    mockRole("Contributor");
    renderSection();

    const rows = await screen.findAllByRole("row");
    expect(rows[1]).toHaveTextContent("XYZ-999");
    expect(rows[2]).toHaveTextContent("ABC-123");
  });

  it("creates a registration via the add-entry form", async () => {
    mockRole("Contributor");
    vi.mocked(registrationsApi.createRegistration).mockResolvedValue({
      ...registrationA,
      id: "reg-3",
      registrationNumber: "NEW-001",
    });

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("ABC-123");
    await user.click(screen.getByRole("button", { name: "Add entry" }));
    await user.type(screen.getByLabelText("Registration number"), "NEW-001");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(registrationsApi.createRegistration).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ registrationNumber: "NEW-001" })
      )
    );
  });

  it("edits and deletes a registration for a Contributor", async () => {
    mockRole("Contributor");
    vi.mocked(registrationsApi.deleteRegistration).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("ABC-123");
    const row = screen.getByText("ABC-123").closest("tr");
    if (!row) {
      throw new Error("Row not found");
    }
    await user.click(within(row).getByRole("button", { name: "Delete" }));

    await waitFor(() =>
      expect(registrationsApi.deleteRegistration).toHaveBeenCalledWith("house-1", "asset-1", "reg-1")
    );
  });

  it("shows an expiry badge for an overdue registration and hides controls for a Viewer", async () => {
    mockRole("Viewer");
    renderSection();

    await screen.findByText("ABC-123");
    expect(screen.getByText("Overdue")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Add entry" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });
});
