import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as assetTypesApi from "@/api/assetTypes";
import * as documentsApi from "@/api/documents";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { AssetListPage } from "@/pages/assets/AssetListPage";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";

vi.mock("@/api/assets");
vi.mock("@/api/assetTypes");
vi.mock("@/api/documents");
vi.mock("@/hooks/useHouseholds");

const car = {
  id: "asset-1",
  householdId: "house-1",
  category: "Car" as const,
  structuralType: "Vehicle" as const,
  name: "Family Car",
  description: null,
  year: 2018,
  coverPhotoId: null,
  usageTrackingMode: "Mileage" as const,
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

const boat = {
  ...car,
  id: "asset-2",
  category: "PowerBoat" as const,
  structuralType: "Boat" as const,
  name: "Sea Ray",
  usageTrackingMode: "Both" as const,
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
    vi.mocked(assetTypesApi.listAssetTypes).mockResolvedValue(testAssetTypeRegistry);
  });

  it("shows an empty state when there are no assets", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([]);

    renderPage();

    expect(await screen.findByText(/No assets yet/)).toBeInTheDocument();
  });

  it("shows registry display labels on asset cards", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car]);

    renderPage();

    await screen.findByText("Family Car");
    expect(screen.getByText(/Car · 2018/)).toBeInTheDocument();
    const card = screen.getByText("Family Car").closest("a");
    expect(card?.querySelector("svg.lucide-car")).toBeInTheDocument();
  });

  it("filters assets by category", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car, boat]);

    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Family Car");
    expect(screen.getByText("Sea Ray")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Power Boat" }));

    await waitFor(() => expect(screen.queryByText("Family Car")).not.toBeInTheDocument());
    expect(screen.getByText("Sea Ray")).toBeInTheDocument();
  });

  it("only offers filter chips for categories present in the household", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car]);

    renderPage();

    await screen.findByText("Family Car");
    expect(screen.getByRole("button", { name: "Car" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Power Boat" })).not.toBeInTheDocument();
  });

  it("hides the add-asset control for a Viewer", async () => {
    mockRole("Viewer");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([]);

    renderPage();

    await screen.findByText(/No assets yet/);
    expect(screen.queryByRole("link", { name: "Add asset" })).not.toBeInTheDocument();
  });

  it("points the add-asset control at the creation wizard route", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([]);

    renderPage();

    await screen.findByText(/No assets yet/);
    expect(screen.getByRole("link", { name: "Add asset" })).toHaveAttribute(
      "href",
      "/households/house-1/assets/new"
    );
  });

  it("points the empty-state add-asset prompt at the creation wizard route", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car]);

    renderPage();

    await screen.findByText("Family Car");
    expect(screen.getByRole("link", { name: "Add Asset" })).toHaveAttribute(
      "href",
      "/households/house-1/assets/new"
    );
  });

  it("shows a cover photo thumbnail when set, and no image otherwise", async () => {
    mockRole("Contributor");
    vi.mocked(documentsApi.downloadDocument).mockResolvedValue(new Blob(["fake"], { type: "image/jpeg" }));
    vi.mocked(assetsApi.listAssets).mockResolvedValue([car, { ...boat, coverPhotoId: "photo-1" }]);

    renderPage();

    await screen.findByText("Family Car");
    const carCard = screen.getByText("Family Car").closest("a");
    const boatCard = screen.getByText("Sea Ray").closest("a");

    expect(carCard?.querySelector("img")).not.toBeInTheDocument();
    await waitFor(() => expect(boatCard?.querySelector("img")).toBeInTheDocument());
  });
});
