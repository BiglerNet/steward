import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as householdsApi from "@/api/households";
import { HouseholdSwitcher } from "@/components/households/HouseholdSwitcher";

vi.mock("@/api/households");

const households = [
  {
    id: "house-1",
    name: "Smith Garage",
    publicSlug: "smith-garage",
    isPublicVisible: false,
    country: null,
    region: null,
    userRole: "Owner" as const,
    storageUsedBytes: 0,
    storageQuotaBytes: 1073741824,
    createdAt: "2026-01-01T00:00:00Z",
  },
  {
    id: "house-2",
    name: "Lake House",
    publicSlug: "lake-house",
    isPublicVisible: false,
    country: null,
    region: null,
    userRole: "Owner" as const,
    storageUsedBytes: 0,
    storageQuotaBytes: 1073741824,
    createdAt: "2026-01-01T00:00:00Z",
  },
];

function CurrentPath() {
  const location = useLocation();
  return <span data-testid="path">{location.pathname}</span>;
}

function renderSwitcher() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/settings"]}>
        <Routes>
          <Route
            path="/households/:householdId/settings"
            element={
              <>
                <HouseholdSwitcher />
                <CurrentPath />
              </>
            }
          />
          <Route
            path="/households/:householdId"
            element={
              <>
                <HouseholdSwitcher />
                <CurrentPath />
              </>
            }
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("HouseholdSwitcher", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(householdsApi.listHouseholds).mockResolvedValue(households);
  });

  it("navigates to the equivalent path under the newly selected household", async () => {
    renderSwitcher();
    const user = userEvent.setup();

    expect(await screen.findByText("Smith Garage")).toBeInTheDocument();

    await user.click(screen.getByText("Smith Garage"));
    await user.click(await screen.findByText("Lake House"));

    expect(await screen.findByText("Lake House")).toBeInTheDocument();
    expect(screen.getByTestId("path").textContent).toBe("/households/house-2/settings");
  });
});
