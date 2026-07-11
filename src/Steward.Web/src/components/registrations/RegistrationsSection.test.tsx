import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as assetTypesApi from "@/api/assetTypes";
import * as regionsApi from "@/api/regions";
import * as registrationsApi from "@/api/registrations";
import type { AssetResponse, HouseholdResponse, RegistrationResponse } from "@/api/types";
import { RegistrationsSection } from "@/components/registrations/RegistrationsSection";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";
import { testRegionRegistry } from "@/test-fixtures/regions";

vi.mock("@/api/registrations");
vi.mock("@/api/assets");
vi.mock("@/api/assetTypes");
vi.mock("@/api/regions");
vi.mock("@/hooks/useHouseholds");

const snowmobile: AssetResponse = {
  id: "asset-1",
  householdId: "house-1",
  category: "Snowmobile",
  structuralType: "Vehicle",
  name: "Trail Blazer",
  description: null,
  year: 2020,
  coverPhotoId: null,
  usageTrackingMode: "Both",
  vin: null,
  make: null,
  model: null,
  color: null,
  trackLengthIn: null,
  hin: null,
  hullMaterial: null,
  hullType: null,
  driveType: null,
  keelType: null,
  mastHeightFt: null,
  mastCount: null,
  lengthFt: null,
  beamFt: null,
  ballSizeIn: null,
  maxLoadLbs: null,
  interiorHeightFt: null,
  interiorLengthFt: null,
  cuttingWidthIn: null,
  maxPsi: null,
  maxGpm: null,
  equipmentDescription: null,
  licensePlate: null,
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
};

const registrationA: RegistrationResponse = {
  id: "reg-1",
  assetId: "asset-1",
  kind: "Registration",
  registrationNumber: "ABC-123",
  issuingAuthority: "Wisconsin",
  validFrom: "2025-01-01",
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
  kind: "Registration",
  registrationNumber: "XYZ-999",
  issuingAuthority: "Wisconsin",
  validFrom: "2026-01-01",
  renewedOn: "2026-01-01",
  cost: 60,
  expiresOn: "2026-12-31",
  notes: null,
  hasDocument: true,
  documentUrl: "/api/.../document",
};

function household(overrides: Partial<HouseholdResponse> = {}): HouseholdResponse {
  return {
    id: "house-1",
    name: "Garage",
    publicSlug: "garage",
    isPublicVisible: false,
    country: null,
    region: null,
    userRole: "Contributor",
    storageUsedBytes: 0,
    storageQuotaBytes: 1073741824,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function mockRole(userRole: "Owner" | "Contributor" | "Viewer", overrides: Partial<HouseholdResponse> = {}) {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [household({ userRole, ...overrides })],
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
    vi.mocked(assetsApi.getAsset).mockResolvedValue(snowmobile);
    vi.mocked(assetTypesApi.listAssetTypes).mockResolvedValue(testAssetTypeRegistry);
    vi.mocked(regionsApi.listRegions).mockResolvedValue(testRegionRegistry);
    vi.mocked(registrationsApi.listRegistrations).mockResolvedValue([registrationB, registrationA]);
  });

  it("renders registrations in the order returned, with kind badges", async () => {
    mockRole("Contributor");
    renderSection();

    const rows = await screen.findAllByRole("row");
    expect(rows[1]).toHaveTextContent("XYZ-999");
    expect(rows[1]).toHaveTextContent("Registration");
    expect(rows[2]).toHaveTextContent("ABC-123");
  });

  it("requires a kind and allows an empty registration number for a trail pass", async () => {
    mockRole("Contributor");
    vi.mocked(registrationsApi.createRegistration).mockResolvedValue({
      ...registrationA,
      id: "reg-3",
      kind: "TrailPass",
      registrationNumber: null,
    });

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("ABC-123");
    await user.click(screen.getByRole("button", { name: "Add entry" }));
    await user.click(screen.getByRole("combobox", { name: "Kind" }));
    await user.click(await screen.findByRole("option", { name: "Trail pass" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(registrationsApi.createRegistration).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ kind: "TrailPass", registrationNumber: null })
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

  it("prefills the Renew form from a record with dates and cost cleared", async () => {
    mockRole("Contributor");
    renderSection();
    const user = userEvent.setup();

    const row = (await screen.findByText("ABC-123")).closest("tr");
    if (!row) {
      throw new Error("Row not found");
    }
    await user.click(within(row).getByRole("button", { name: "Renew" }));

    expect(screen.getByLabelText("Registration #")).toHaveValue("ABC-123");
    expect(screen.getByLabelText("Valid from")).toHaveValue("");
    expect(screen.getByLabelText("Renewed on")).toHaveValue("");
    expect(screen.getByLabelText("Expires on")).toHaveValue("");
    expect(screen.getByLabelText("Cost")).toHaveValue(null);
    expect(screen.getByText("Add registration entry")).toBeInTheDocument();
  });

  it("shows a permit nudge for a kind the category typically needs with none current, and clears once one exists", async () => {
    mockRole("Contributor");
    vi.mocked(registrationsApi.listRegistrations).mockResolvedValue([registrationA]);

    const { unmount } = renderSection();

    expect(await screen.findByText(/usually need a trail pass/i)).toBeInTheDocument();
    unmount();

    vi.mocked(registrationsApi.listRegistrations).mockResolvedValue([
      registrationA,
      { ...registrationB, kind: "TrailPass", expiresOn: "2099-12-31" },
    ]);
    renderSection();

    await screen.findByText("ABC-123");
    expect(screen.queryByText(/usually need a trail pass/i)).not.toBeInTheDocument();
  });

  it("suggests the household's own region first in the issuing-authority combobox", async () => {
    mockRole("Contributor", { country: "US", region: "US-WI" });
    renderSection();
    const user = userEvent.setup();

    await screen.findByText("ABC-123");
    await user.click(screen.getByRole("button", { name: "Add entry" }));
    await user.click(screen.getByLabelText("Issuing authority"));

    const options = await screen.findAllByRole("button", { name: /Wisconsin|Minnesota|California|Ontario|British Columbia/ });
    expect(options[0]).toHaveTextContent("Wisconsin");
  });
});
