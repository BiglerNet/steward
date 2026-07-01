import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { AssetDetailLayout } from "@/pages/assets/AssetDetailLayout";

vi.mock("@/api/assets");
vi.mock("@/hooks/useHouseholds");

const car = {
  id: "asset-1",
  householdId: "house-1",
  assetType: "Car" as const,
  name: "Family Car",
  description: null,
  year: 2018,
  photoUrl: null,
  usageTrackingMode: "Mileage" as const,
  vin: "1HGCM82633A123456",
  color: "Blue",
  make: "Honda",
  model: "Civic",
  hin: null,
  hullMaterial: null,
  lengthFt: null,
  beamFt: null,
  trackLengthIn: null,
  ballSizeIn: null,
  maxLoadLbs: null,
  interiorHeightFt: null,
  interiorLengthFt: null,
  cuttingWidthIn: null,
  maxPsi: null,
  maxGpm: null,
  equipmentDescription: null,
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
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

function renderLayout() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/engines"]}>
        <Routes>
          <Route path="/households/:householdId/assets/:assetId" element={<AssetDetailLayout />}>
            <Route path="engines" element={<div>Engines tab content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AssetDetailLayout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(assetsApi.getAsset).mockResolvedValue(car);
  });

  it("shows the Edit control for a Contributor but not Delete", async () => {
    mockRole("Contributor");

    renderLayout();

    await screen.findByText("Family Car");
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });

  it("shows both Edit and Delete controls for an Owner", async () => {
    mockRole("Owner");

    renderLayout();

    await screen.findByText("Family Car");
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete" })).toBeInTheDocument();
  });

  it("hides Edit and Delete controls for a Viewer", async () => {
    mockRole("Viewer");

    renderLayout();

    await screen.findByText("Family Car");
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });
});
