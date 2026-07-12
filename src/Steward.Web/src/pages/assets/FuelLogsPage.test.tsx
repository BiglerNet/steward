import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as enginesApi from "@/api/engines";
import * as trackingApi from "@/api/tracking";
import type { EngineResponse } from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { FuelLogsPage } from "@/pages/assets/FuelLogsPage";

vi.mock("@/api/engines");
vi.mock("@/api/tracking");
vi.mock("@/hooks/useHouseholds");

function baseEngine(overrides: Partial<EngineResponse> = {}): EngineResponse {
  return {
    id: "engine-1",
    assetId: "asset-1",
    label: "Main engine",
    make: null,
    model: null,
    serialNumber: null,
    year: null,
    engineType: "Ice",
    mechanism: null,
    fuelType: "Gasoline",
    isExternallyChargeable: null,
    twoStrokeOilDelivery: null,
    twoStrokeMixRatio: null,
    cylinders: null,
    displacementCc: null,
    status: "Active",
    installedDate: null,
    installedAtAssetMiles: null,
    installedAtAssetHours: null,
    horsepowerHp: null,
    torqueNm: null,
    oilCapacityL: null,
    recommendedOilType: null,
    coolantCapacityL: null,
    recommendedOctane: null,
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
        userRole: "Contributor",
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);

  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/fuel-logs"]}>
        <Routes>
          <Route path="/households/:householdId/assets/:assetId/fuel-logs" element={<FuelLogsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("FuelLogsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(trackingApi.listFuelLogs).mockResolvedValue([]);
  });

  it("auto-selects a single loggable Ice engine with no selector shown", async () => {
    vi.mocked(enginesApi.listEngines).mockResolvedValue([baseEngine()]);
    vi.mocked(trackingApi.createFuelLog).mockResolvedValue({
      id: "log-1",
      assetId: "asset-1",
      engineId: "engine-1",
      logType: "Fillup",
      date: "2026-06-01",
      quantity: 10,
      unit: "Gallons",
      fuelGrade: null,
      pricePerUnit: null,
      totalCost: null,
      milesAtLog: null,
      hoursAtLog: null,
      notes: null,
    });
    const user = userEvent.setup();

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.queryByRole("combobox", { name: "Engine" })).not.toBeInTheDocument();
    expect(screen.queryByRole("option", { name: "Kwh" })).not.toBeInTheDocument();

    await user.type(screen.getByLabelText("Date"), "2026-06-01");
    await user.type(screen.getByLabelText("Quantity"), "10");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(trackingApi.createFuelLog).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ engineId: "engine-1", unit: "Gallons" })
      )
    );
  });

  it("never prompts for engine choice on a conventional (non-plug-in) hybrid", async () => {
    vi.mocked(enginesApi.listEngines).mockResolvedValue([
      baseEngine({ id: "ice-1", label: "Gas engine" }),
      baseEngine({
        id: "electric-1",
        label: "Motor",
        engineType: "Electric",
        fuelType: null,
        isExternallyChargeable: false,
      }),
    ]);
    vi.mocked(trackingApi.createFuelLog).mockResolvedValue({
      id: "log-1",
      assetId: "asset-1",
      engineId: "ice-1",
      logType: "Fillup",
      date: "2026-06-01",
      quantity: 10,
      unit: "Gallons",
      fuelGrade: null,
      pricePerUnit: null,
      totalCost: null,
      milesAtLog: null,
      hoursAtLog: null,
      notes: null,
    });
    const user = userEvent.setup();

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.queryByRole("combobox", { name: "Engine" })).not.toBeInTheDocument();

    await user.type(screen.getByLabelText("Date"), "2026-06-01");
    await user.type(screen.getByLabelText("Quantity"), "10");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(trackingApi.createFuelLog).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ engineId: "ice-1" })
      )
    );
  });

  it("requires picking an engine for a plug-in hybrid and constrains unit to the selection", async () => {
    vi.mocked(enginesApi.listEngines).mockResolvedValue([
      baseEngine({ id: "ice-1", label: "Gas engine" }),
      baseEngine({
        id: "electric-1",
        label: "Electric motor",
        engineType: "Electric",
        fuelType: null,
        isExternallyChargeable: true,
      }),
    ]);
    const user = userEvent.setup();

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.getByRole("combobox", { name: "Engine" })).toBeInTheDocument();

    await user.type(screen.getByLabelText("Date"), "2026-06-01");
    await user.type(screen.getByLabelText("Quantity"), "62");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(trackingApi.createFuelLog).not.toHaveBeenCalled();

    await user.click(screen.getByRole("combobox", { name: "Engine" }));
    await user.click(await screen.findByRole("option", { name: "Electric motor" }));

    expect(screen.getByRole("combobox", { name: "Unit" })).toHaveTextContent("Kwh");
  });

  it("keeps free unit choice and no selector for an asset with no modeled engines", async () => {
    vi.mocked(enginesApi.listEngines).mockResolvedValue([]);
    const user = userEvent.setup();

    renderPage();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.queryByRole("combobox", { name: "Engine" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("combobox", { name: "Unit" }));
    expect(await screen.findByRole("option", { name: "Kwh" })).toBeInTheDocument();
  });
});
