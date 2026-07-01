import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { AssetListPage } from "@/pages/assets/AssetListPage";

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
  vin: null,
  color: null,
  make: null,
  model: null,
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

function renderPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets"]}>
        <Routes>
          <Route path="/households/:householdId/assets" element={<AssetListPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AssetListPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows an empty state when there are no assets", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText(/No assets yet/)).toBeInTheDocument();
  });

  it("filters assets by type", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car]);

    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Family Car");
    expect(assetsApi.listAssets).toHaveBeenCalledWith("house-1", undefined);

    await user.click(screen.getByRole("button", { name: "Boat" }));

    await waitFor(() => expect(assetsApi.listAssets).toHaveBeenCalledWith("house-1", "Boat"));
  });

  it("hides the add-asset control for a Viewer", async () => {
    mockRole("Viewer");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([]);

    renderPage();

    await screen.findByText(/No assets yet/);
    expect(screen.queryByRole("button", { name: "Add asset" })).not.toBeInTheDocument();
  });
});
