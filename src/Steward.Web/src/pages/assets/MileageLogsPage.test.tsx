import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as trackingApi from "@/api/tracking";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { MileageLogsPage } from "@/pages/assets/MileageLogsPage";

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
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/mileage-logs"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/mileage-logs"
            element={<MileageLogsPage />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("MileageLogsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(trackingApi.listMileageLogs).mockResolvedValue([]);
  });

  it("has no engine selector when adding a mileage log", async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(await screen.findByRole("button", { name: "Add entry" }));

    expect(screen.getByLabelText("Date")).toBeInTheDocument();
    expect(screen.queryByLabelText("Engine (optional)")).not.toBeInTheDocument();
    expect(screen.queryByText(/engine/i)).not.toBeInTheDocument();
  });
});
