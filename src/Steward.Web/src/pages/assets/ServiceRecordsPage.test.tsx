import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as enginesApi from "@/api/engines";
import * as trackingApi from "@/api/tracking";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { ServiceRecordsPage } from "@/pages/assets/ServiceRecordsPage";

vi.mock("@/api/engines");
vi.mock("@/api/tracking");
vi.mock("@/hooks/useHouseholds");

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
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/service-records"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/service-records"
            element={<ServiceRecordsPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("ServiceRecordsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(trackingApi.listServiceRecords).mockResolvedValue([]);
    vi.mocked(enginesApi.listEngines).mockResolvedValue([
      {
        id: "engine-1",
        assetId: "asset-1",
        label: "Main engine",
        make: null,
        model: null,
        serialNumber: null,
        year: null,
        engineType: "Ice",
        fuelType: "Gasoline",
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
      },
    ]);
  });

  it("shows an engine selector when adding a service record", async () => {
    renderPage();
    const user = (await import("@testing-library/user-event")).default.setup();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.getByLabelText("Engine (optional)")).toBeInTheDocument();
  });
});
