import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as enginesApi from "@/api/engines";
import * as maintenanceItemsApi from "@/api/maintenanceItems";
import type { AssetResponse, ChecklistItemResponse, MaintenanceItemResponse } from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { MaintenanceItemEditorPage } from "@/pages/maintenance/MaintenanceItemEditorPage";

vi.mock("@/api/maintenanceItems");
vi.mock("@/api/engines");
vi.mock("@/api/assets");
vi.mock("@/hooks/useHouseholds");

function checklistItem(overrides: Partial<ChecklistItemResponse> = {}): ChecklistItemResponse {
  return {
    id: "c-1",
    maintenanceItemId: "item-1",
    text: "Drain oil",
    status: "Open",
    resolvedAt: null,
    sortOrder: 0,
    engineId: null,
    templateStepId: null,
    ...overrides,
  };
}

function baseItem(overrides: Partial<MaintenanceItemResponse> = {}): MaintenanceItemResponse {
  return {
    id: "item-1",
    assetId: "asset-1",
    engineId: null,
    templateId: null,
    title: "Winterize",
    description: null,
    providerName: null,
    status: "InProgress",
    date: null,
    cost: null,
    odometerMiles: null,
    engineHours: null,
    isBlocked: false,
    completedAt: null,
    checklistItems: [checklistItem({ id: "c-1" }), checklistItem({ id: "c-2", text: "Fog engine" })],
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

function renderPage(
  item: MaintenanceItemResponse,
  options: { state?: { from: string; fromLabel: string } } = {}
) {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [
      {
        id: "house-1",
        name: "Garage",
        publicSlug: "garage",
        isPublicVisible: false,
        country: null,
        region: null,
        userRole: "Contributor",
        storageUsedBytes: 0,
        storageQuotaBytes: 0,
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
  vi.mocked(enginesApi.listEngines).mockResolvedValue([]);
  vi.mocked(maintenanceItemsApi.getMaintenanceItem).mockResolvedValue(item);
  vi.mocked(assetsApi.getAsset).mockResolvedValue(asset());

  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter
        initialEntries={[
          {
            pathname: "/households/house-1/assets/asset-1/maintenance/item-1",
            state: options.state,
          },
        ]}
      >
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/maintenance/:itemId"
            element={<MaintenanceItemEditorPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("MaintenanceItemEditorPage — Done-transition confirmation", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("prompts when completing an item with open checklist items", async () => {
    const user = userEvent.setup();
    renderPage(baseItem());

    await user.click(await screen.findByRole("combobox", { name: "Status" }));
    await user.click(await screen.findByRole("option", { name: "Done" }));

    expect(await screen.findByText("Complete with open items?")).toBeInTheDocument();
  });

  it("Go back cancels the transition without changing anything", async () => {
    const user = userEvent.setup();
    renderPage(baseItem());

    await user.click(await screen.findByRole("combobox", { name: "Status" }));
    await user.click(await screen.findByRole("option", { name: "Done" }));
    await user.click(await screen.findByRole("button", { name: "Go back" }));

    expect(maintenanceItemsApi.patchMaintenanceItem).not.toHaveBeenCalled();
    expect(maintenanceItemsApi.patchChecklistItem).not.toHaveBeenCalled();
    expect(screen.queryByText("Complete with open items?")).not.toBeInTheDocument();
  });

  it("Complete anyway sets the item Done and leaves checklist items untouched", async () => {
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(baseItem({ status: "Done" }));
    const user = userEvent.setup();
    renderPage(baseItem());

    await user.click(await screen.findByRole("combobox", { name: "Status" }));
    await user.click(await screen.findByRole("option", { name: "Done" }));
    await user.click(await screen.findByRole("button", { name: "Complete anyway" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        "item-1",
        { status: "Done" }
      )
    );
    expect(maintenanceItemsApi.patchChecklistItem).not.toHaveBeenCalled();
  });

  it("Mark remaining as Skipped, then complete skips every open item before completing", async () => {
    vi.mocked(maintenanceItemsApi.patchChecklistItem).mockResolvedValue(checklistItem({ status: "Skipped" }));
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(baseItem({ status: "Done" }));
    const user = userEvent.setup();
    renderPage(baseItem());

    await user.click(await screen.findByRole("combobox", { name: "Status" }));
    await user.click(await screen.findByRole("option", { name: "Done" }));
    await user.click(await screen.findByRole("button", { name: "Mark remaining as Skipped, then complete" }));

    await waitFor(() => expect(maintenanceItemsApi.patchChecklistItem).toHaveBeenCalledTimes(2));
    expect(maintenanceItemsApi.patchChecklistItem).toHaveBeenCalledWith(
      "house-1",
      "asset-1",
      "item-1",
      "c-1",
      { status: "Skipped" }
    );
    expect(maintenanceItemsApi.patchChecklistItem).toHaveBeenCalledWith(
      "house-1",
      "asset-1",
      "item-1",
      "c-2",
      { status: "Skipped" }
    );
    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        "item-1",
        { status: "Done" }
      )
    );
  });

  it("does not prompt when no checklist items are open", async () => {
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(baseItem({ status: "Done" }));
    const user = userEvent.setup();
    renderPage(baseItem({ checklistItems: [checklistItem({ status: "Skipped" })] }));

    await user.click(await screen.findByRole("combobox", { name: "Status" }));
    await user.click(await screen.findByRole("option", { name: "Done" }));

    expect(screen.queryByText("Complete with open items?")).not.toBeInTheDocument();
    await waitFor(() => expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalled());
  });
});

describe("MaintenanceItemEditorPage — breadcrumb", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows the kanban origin label and links back to it when arriving from the board", async () => {
    renderPage(baseItem(), { state: { from: "/households/house-1/maintenance?asset=asset-1", fromLabel: "Maintenance" } });

    const crumb = await screen.findByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Maintenance");
    expect(crumb).toHaveTextContent("Winterize");
    expect(screen.getByRole("link", { name: "Maintenance" })).toHaveAttribute(
      "href",
      "/households/house-1/maintenance?asset=asset-1"
    );
  });

  it("shows the asset's name and links back to its Maintenance tab when arriving from there", async () => {
    renderPage(baseItem(), { state: { from: "/households/house-1/assets/asset-1/maintenance", fromLabel: "Trail Blazer" } });

    const crumb = await screen.findByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Trail Blazer");
    expect(screen.getByRole("link", { name: "Trail Blazer" })).toHaveAttribute(
      "href",
      "/households/house-1/assets/asset-1/maintenance"
    );
  });

  it("falls back to the asset name and Maintenance tab when there is no navigation state", async () => {
    renderPage(baseItem());

    const crumb = await screen.findByRole("navigation", { name: "Breadcrumb" });
    await waitFor(() => expect(crumb).toHaveTextContent("Trail Blazer"));
    expect(crumb).toHaveTextContent("Maintenance");
    expect(screen.getAllByRole("link", { name: "Trail Blazer" })[0]).toHaveAttribute(
      "href",
      "/households/house-1/assets/asset-1/maintenance"
    );
  });
});

describe("MaintenanceItemEditorPage — completed date", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a read-only Completed field with the absolute date when the item has one", async () => {
    renderPage(baseItem({ completedAt: "2026-01-05T00:00:00Z" }));

    expect(await screen.findByText("Completed")).toBeInTheDocument();
    expect(screen.getByText(new Date("2026-01-05T00:00:00Z").toLocaleDateString())).toBeInTheDocument();
  });

  it("omits the Completed field entirely for an item never completed", async () => {
    renderPage(baseItem({ completedAt: null }));

    await screen.findByText("Winterize");
    expect(screen.queryByText("Completed")).not.toBeInTheDocument();
  });
});
