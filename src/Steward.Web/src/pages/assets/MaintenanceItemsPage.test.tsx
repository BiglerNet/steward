import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as maintenanceItemsApi from "@/api/maintenanceItems";
import * as maintenanceScheduleApi from "@/api/maintenanceSchedule";
import type { AssetResponse, MaintenanceItemResponse } from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { MaintenanceItemsPage } from "@/pages/assets/MaintenanceItemsPage";

vi.mock("@/api/maintenanceItems");
vi.mock("@/api/maintenanceSchedule");
vi.mock("@/api/assets");
vi.mock("@/hooks/useHouseholds");

const mockNavigate = vi.fn();
vi.mock("react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router")>();
  return { ...actual, useNavigate: () => mockNavigate };
});

function item(overrides: Partial<MaintenanceItemResponse> = {}): MaintenanceItemResponse {
  return {
    id: "item-1",
    assetId: "asset-1",
    engineId: null,
    templateId: null,
    title: "Oil change",
    description: null,
    providerName: null,
    status: "Planned",
    date: null,
    cost: null,
    odometerMiles: null,
    engineHours: null,
    isBlocked: false,
    completedAt: null,
    checklistItems: [],
    partLines: [],
    ...overrides,
  };
}

function asset(overrides: Partial<AssetResponse> = {}): AssetResponse {
  return {
    id: "asset-1",
    householdId: "house-1",
    category: "Snowmobile",
    structuralType: "Vehicle",
    name: "Trail Blazer",
    description: null,
    year: null,
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
    powertrain: null,
    ...overrides,
  };
}

function renderPage() {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [
      {
        id: "house-1",
        name: "Garage",
        publicSlug: "garage",
        isPublicVisible: false,
        country: null,
        region: null,
        userRole: "Viewer",
        storageUsedBytes: 0,
        storageQuotaBytes: 0,
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
  vi.mocked(assetsApi.getAsset).mockResolvedValue(asset());
  vi.mocked(maintenanceScheduleApi.getMaintenanceSchedule).mockResolvedValue([]);

  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/maintenance"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/maintenance"
            element={<MaintenanceItemsPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("MaintenanceItemsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows the absolute completed date for a completed item", async () => {
    vi.mocked(maintenanceItemsApi.listMaintenanceItems).mockResolvedValue([
      item({ status: "Done", completedAt: "2026-01-05T00:00:00Z" }),
    ]);

    renderPage();

    expect(await screen.findByText("Completed")).toBeInTheDocument();
    expect(screen.getByText(new Date("2026-01-05T00:00:00Z").toLocaleDateString())).toBeInTheDocument();
  });

  it("shows a placeholder in the Completed column for an item never completed", async () => {
    vi.mocked(maintenanceItemsApi.listMaintenanceItems).mockResolvedValue([item({ completedAt: null })]);

    renderPage();

    await screen.findByText("Oil change");
    const row = screen.getByText("Oil change").closest("tr");
    expect(row).toHaveTextContent("—");
  });

  it("passes the asset name and its Maintenance tab path as navigation state to the item editor", async () => {
    vi.mocked(maintenanceItemsApi.listMaintenanceItems).mockResolvedValue([item()]);

    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByText("Oil change"));

    expect(mockNavigate).toHaveBeenCalledWith(
      "/households/house-1/assets/asset-1/maintenance/item-1",
      {
        state: {
          from: "/households/house-1/assets/asset-1/maintenance",
          fromLabel: "Trail Blazer",
        },
      }
    );
  });
});
