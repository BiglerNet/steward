import { act } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { DragEndEvent } from "@dnd-kit/core";
import * as assetsApi from "@/api/assets";
import * as maintenanceItemsApi from "@/api/maintenanceItems";
import type { AssetResponse, HouseholdMaintenanceItemResponse } from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { MaintenanceKanbanPage } from "@/pages/maintenance/MaintenanceKanbanPage";

vi.mock("@/api/maintenanceItems");
vi.mock("@/api/assets");
vi.mock("@/hooks/useHouseholds");

const mockNavigate = vi.fn();
vi.mock("react-router", async (importOriginal) => {
  const actual = await importOriginal<typeof import("react-router")>();
  return { ...actual, useNavigate: () => mockNavigate };
});

let capturedOnDragEnd: ((event: DragEndEvent) => void) | null = null;

vi.mock("@dnd-kit/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@dnd-kit/core")>();
  return {
    ...actual,
    DndContext: (props: { onDragEnd: (event: DragEndEvent) => void; children: React.ReactNode }) => {
      capturedOnDragEnd = props.onDragEnd;
      return props.children;
    },
  };
});

function simulateDrop(itemId: string, overId: string) {
  return act(async () => {
    capturedOnDragEnd?.({
      active: { id: itemId, data: { current: undefined }, rect: { current: { initial: null, translated: null } } },
      over: { id: overId, rect: {} as never, data: { current: undefined }, disabled: false },
      activatorEvent: new Event("pointerdown"),
      collisions: null,
      delta: { x: 0, y: 0 },
    } as unknown as DragEndEvent);
  });
}

function item(overrides: Partial<HouseholdMaintenanceItemResponse> = {}): HouseholdMaintenanceItemResponse {
  return {
    id: "item-1",
    assetId: "asset-1",
    assetName: "Trail Blazer",
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
        storageUsedBytes: 0,
        storageQuotaBytes: 0,
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
}

function renderPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/maintenance"]}>
        <Routes>
          <Route path="/households/:householdId/maintenance" element={<MaintenanceKanbanPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("MaintenanceKanbanPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    capturedOnDragEnd = null;
    vi.mocked(assetsApi.listAssets).mockResolvedValue([asset()]);
  });

  it("drags a card between Planned and InProgress columns", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([item({ status: "Planned" })]);
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(item({ status: "InProgress" }));

    renderPage();
    await screen.findByText("Oil change");

    await simulateDrop("item-1", "InProgress");

    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        "item-1",
        { status: "InProgress" }
      )
    );
  });

  it("drags directly to Done when there are no open checklist items", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([
      item({ checklistItems: [] }),
    ]);
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(item({ status: "Done" }));

    renderPage();
    await screen.findByText("Oil change");

    await simulateDrop("item-1", "Done");

    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        "item-1",
        { status: "Done" }
      )
    );
    expect(screen.queryByText("Complete with open items?")).not.toBeInTheDocument();
  });

  it("prompts before completing a card with open checklist items, and Go back leaves it unchanged", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([
      item({ checklistItems: [{ id: "c-1", maintenanceItemId: "item-1", text: "Step", status: "Open", resolvedAt: null, sortOrder: 0, engineId: null, templateStepId: null }] }),
    ]);

    const user = userEvent.setup();
    renderPage();
    await screen.findByText("Oil change");

    await simulateDrop("item-1", "Done");

    expect(await screen.findByText("Complete with open items?")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Go back" }));

    expect(maintenanceItemsApi.patchMaintenanceItem).not.toHaveBeenCalled();
    expect(screen.queryByText("Complete with open items?")).not.toBeInTheDocument();
  });

  it("Complete anyway completes the card and removes it from the board", async () => {
    mockRole("Contributor");
    const openItem = item({
      checklistItems: [{ id: "c-1", maintenanceItemId: "item-1", text: "Step", status: "Open", resolvedAt: null, sortOrder: 0, engineId: null, templateStepId: null }],
    });
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems)
      .mockResolvedValueOnce([openItem])
      .mockResolvedValueOnce([]);
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(item({ status: "Done" }));

    const user = userEvent.setup();
    renderPage();
    await screen.findByText("Oil change");

    await simulateDrop("item-1", "Done");
    await screen.findByText("Complete with open items?");
    await user.click(screen.getByRole("button", { name: "Complete anyway" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        "item-1",
        { status: "Done" }
      )
    );
    expect(maintenanceItemsApi.patchChecklistItem).not.toHaveBeenCalled();
    await waitFor(() => expect(screen.queryByText("Oil change")).not.toBeInTheDocument());
  });

  it("Mark remaining as Skipped, then complete skips open items before completing", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([
      item({
        checklistItems: [{ id: "c-1", maintenanceItemId: "item-1", text: "Step", status: "Open", resolvedAt: null, sortOrder: 0, engineId: null, templateStepId: null }],
      }),
    ]);
    vi.mocked(maintenanceItemsApi.patchChecklistItem).mockResolvedValue({
      id: "c-1", maintenanceItemId: "item-1", text: "Step", status: "Skipped", resolvedAt: "2026-01-01T00:00:00Z", sortOrder: 0, engineId: null, templateStepId: null,
    });
    vi.mocked(maintenanceItemsApi.patchMaintenanceItem).mockResolvedValue(item({ status: "Done" }));

    const user = userEvent.setup();
    renderPage();
    await screen.findByText("Oil change");

    await simulateDrop("item-1", "Done");
    await screen.findByText("Complete with open items?");
    await user.click(screen.getByRole("button", { name: "Mark remaining as Skipped, then complete" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.patchChecklistItem).toHaveBeenCalledWith(
        "house-1", "asset-1", "item-1", "c-1", { status: "Skipped" }
      )
    );
    await waitFor(() =>
      expect(maintenanceItemsApi.patchMaintenanceItem).toHaveBeenCalledWith(
        "house-1", "asset-1", "item-1", { status: "Done" }
      )
    );
  });

  it("filters the board by asset", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([asset(), asset({ id: "asset-2", name: "Second Sled" })]);
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([item()]);

    const user = userEvent.setup();
    renderPage();
    await screen.findByText("Oil change");

    await user.click(screen.getByRole("combobox", { name: "Filter by asset" }));
    await user.click(await screen.findByRole("option", { name: "Second Sled" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.listHouseholdMaintenanceItems).toHaveBeenLastCalledWith(
        "house-1",
        { status: ["Planned", "InProgress", "Done"], assetId: "asset-2" }
      )
    );
  });

  it("shows items completed within the last 7 days in the Done column, but not older ones", async () => {
    mockRole("Contributor");
    const now = new Date();
    const recentlyDone = item({
      id: "item-recent",
      title: "Recently completed",
      status: "Done",
      completedAt: new Date(now.getTime() - 2 * 24 * 60 * 60 * 1000).toISOString(),
    });
    const oldDone = item({
      id: "item-old",
      title: "Completed long ago",
      status: "Done",
      completedAt: new Date(now.getTime() - 10 * 24 * 60 * 60 * 1000).toISOString(),
    });
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([recentlyDone, oldDone]);

    renderPage();

    expect(await screen.findByText("Recently completed")).toBeInTheDocument();
    expect(screen.queryByText("Completed long ago")).not.toBeInTheDocument();
  });

  it("does not make the card draggable or show a cancel action for cards in the Done column", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([
      item({ status: "Done", completedAt: new Date().toISOString() }),
    ]);

    renderPage();
    await screen.findByText("Oil change");

    const card = document.querySelector('[data-item-id="item-1"]');
    expect(card).not.toHaveAttribute("role", "button");
    expect(screen.queryByLabelText(/"Oil change" actions/)).not.toBeInTheDocument();
  });

  it("shows a relative completed-at label on a Done-zone card", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([
      item({
        status: "Done",
        completedAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      }),
    ]);

    renderPage();

    expect(await screen.findByText("Completed 2 days ago")).toBeInTheDocument();
  });

  it("navigates to the item's detail page on a plain click of its title, passing the board as navigation origin", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([item()]);

    const user = userEvent.setup();
    renderPage();

    await user.click(await screen.findByRole("button", { name: "Oil change" }));

    expect(mockNavigate).toHaveBeenCalledWith(
      "/households/house-1/assets/asset-1/maintenance/item-1",
      { state: { from: "/households/house-1/maintenance", fromLabel: "Maintenance" } }
    );
  });

  it("makes the whole card body a drag surface for an editor, and opens the menu on a plain click", async () => {
    mockRole("Contributor");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([item()]);

    const user = userEvent.setup();
    renderPage();
    await screen.findByText("Oil change");

    const card = document.querySelector('[data-item-id="item-1"]');
    expect(card).toHaveAttribute("role", "button");
    expect(card).toHaveAttribute("aria-roledescription", "draggable");

    await user.click(screen.getByLabelText('"Oil change" actions'));
    expect(await screen.findByText("Cancel")).toBeInTheDocument();
  });

  it("hides draggability and card actions for a Viewer", async () => {
    mockRole("Viewer");
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([item()]);

    renderPage();
    await screen.findByText("Oil change");

    const card = document.querySelector('[data-item-id="item-1"]');
    expect(card).not.toHaveAttribute("role", "button");
    expect(screen.queryByLabelText(/"Oil change" actions/)).not.toBeInTheDocument();
  });

  it("restores the asset filter from the URL on load", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.listAssets).mockResolvedValue([asset(), asset({ id: "asset-2", name: "Second Sled" })]);
    vi.mocked(maintenanceItemsApi.listHouseholdMaintenanceItems).mockResolvedValue([]);

    const queryClient = new QueryClient();
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={["/households/house-1/maintenance?asset=asset-2"]}>
          <Routes>
            <Route path="/households/:householdId/maintenance" element={<MaintenanceKanbanPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    await waitFor(() =>
      expect(maintenanceItemsApi.listHouseholdMaintenanceItems).toHaveBeenCalledWith(
        "house-1",
        { status: ["Planned", "InProgress", "Done"], assetId: "asset-2" }
      )
    );
    expect(await screen.findByRole("combobox", { name: "Filter by asset" })).toHaveTextContent("Second Sled");
  });
});
