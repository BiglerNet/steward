import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as enginesApi from "@/api/engines";
import { EnginesSection } from "@/components/assets/EnginesSection";
import * as useHouseholdsModule from "@/hooks/useHouseholds";

vi.mock("@/api/engines");
vi.mock("@/hooks/useHouseholds");

const engine = {
  id: "engine-1",
  assetId: "asset-1",
  label: "Main engine",
  make: "Mercury",
  model: "150",
  serialNumber: null,
  year: 2020,
  engineType: "Ice" as const,
  mechanism: null,
  fuelType: "Gasoline" as const,
  isExternallyChargeable: null,
  twoStrokeOilDelivery: null,
  twoStrokeMixRatio: null,
  cylinders: null,
  displacementCc: null,
  status: "Active" as const,
  installedDate: null,
  installedAtAssetMiles: null,
  installedAtAssetHours: null,
  horsepowerHp: null,
  torqueNm: null,
  oilCapacityL: null,
  recommendedOilType: null,
  coolantCapacityL: null,
  recommendedOctane: null,
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
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/engines"]}>
        <Routes>
          <Route path="/households/:householdId/assets/:assetId/engines" element={<EnginesSection />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("EnginesSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(enginesApi.listEngines).mockResolvedValue([engine]);
  });

  it("creates an engine via the add-engine form (Contributor)", async () => {
    mockRole("Contributor");
    vi.mocked(enginesApi.createEngine).mockResolvedValue({ ...engine, id: "engine-2", label: "Second engine" });

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Main engine");
    await user.click(screen.getByRole("button", { name: "Add engine" }));
    await user.type(screen.getByLabelText("Label"), "Second engine");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(enginesApi.createEngine).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ label: "Second engine" })
      )
    );
  });

  it("allows a Contributor to edit but not delete an engine", async () => {
    mockRole("Contributor");

    renderSection();

    await screen.findByText("Main engine");
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });

  it("allows an Owner to delete an engine", async () => {
    mockRole("Owner");
    vi.mocked(enginesApi.deleteEngine).mockResolvedValue(undefined);
    vi.spyOn(window, "confirm").mockReturnValue(true);

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Main engine");
    await user.click(screen.getByRole("button", { name: "Delete" }));

    await waitFor(() => expect(enginesApi.deleteEngine).toHaveBeenCalledWith("house-1", "asset-1", "engine-1"));
  });

  it("hides all engine controls for a Viewer", async () => {
    mockRole("Viewer");

    renderSection();

    await screen.findByText("Main engine");
    expect(screen.queryByRole("button", { name: "Add engine" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });

  it("shows Mechanism and Fuel type fields only when engine type is Ice", async () => {
    mockRole("Contributor");

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Main engine");
    await user.click(screen.getByRole("button", { name: "Add engine" }));

    expect(screen.getByRole("combobox", { name: "Mechanism" })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Fuel type" })).toBeInTheDocument();
    expect(screen.queryByLabelText("Externally chargeable")).not.toBeInTheDocument();

    await user.click(screen.getByRole("combobox", { name: "Engine type" }));
    await user.click(await screen.findByRole("option", { name: "Electric" }));

    expect(screen.queryByRole("combobox", { name: "Mechanism" })).not.toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Fuel type" })).not.toBeInTheDocument();
    expect(screen.getByLabelText("Externally chargeable")).toBeInTheDocument();
  });

  it("shows two-stroke oil fields only when mechanism is TwoStroke", async () => {
    mockRole("Contributor");

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Main engine");
    await user.click(screen.getByRole("button", { name: "Add engine" }));

    expect(screen.queryByRole("combobox", { name: "Two-stroke oil delivery" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Mix ratio")).not.toBeInTheDocument();

    await user.click(screen.getByRole("combobox", { name: "Mechanism" }));
    await user.click(await screen.findByRole("option", { name: "TwoStroke" }));

    expect(screen.getByRole("combobox", { name: "Two-stroke oil delivery" })).toBeInTheDocument();
    expect(screen.getByLabelText("Mix ratio")).toBeInTheDocument();
  });

  it("retires an Active engine via the Retire action", async () => {
    mockRole("Contributor");
    vi.mocked(enginesApi.retireEngine).mockResolvedValue({ ...engine, status: "Retired" });

    renderSection();
    const user = userEvent.setup();

    await screen.findByText("Main engine");
    expect(screen.queryByRole("button", { name: "Reactivate" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Retire" }));

    await waitFor(() => expect(enginesApi.retireEngine).toHaveBeenCalledWith("house-1", "asset-1", "engine-1"));
  });

  it("only offers Reactivate on a Retired engine", async () => {
    mockRole("Contributor");
    vi.mocked(enginesApi.listEngines).mockResolvedValue([{ ...engine, status: "Retired" }]);

    renderSection();

    await screen.findByText("Main engine");
    expect(screen.getByRole("button", { name: "Reactivate" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Retire" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Mark broken" })).not.toBeInTheDocument();
  });
});
