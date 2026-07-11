import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as householdsApi from "@/api/households";
import * as regionsApi from "@/api/regions";
import type { HouseholdResponse } from "@/api/types";
import { HouseholdLocationForm } from "@/components/households/HouseholdLocationForm";
import { testRegionRegistry } from "@/test-fixtures/regions";

vi.mock("@/api/households");
vi.mock("@/api/regions");

const household: HouseholdResponse = {
  id: "house-1",
  name: "Smith Garage",
  publicSlug: "smith-garage",
  isPublicVisible: false,
  country: "US",
  region: "US-WI",
  userRole: "Owner",
  storageUsedBytes: 0,
  storageQuotaBytes: 1073741824,
  createdAt: "2026-01-01T00:00:00Z",
};

function renderForm(canEdit: boolean, initial: HouseholdResponse = household) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <HouseholdLocationForm household={initial} canEdit={canEdit} />
    </QueryClientProvider>
  );
}

describe("HouseholdLocationForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(regionsApi.listRegions).mockResolvedValue(testRegionRegistry);
  });

  it("sets a new country and region", async () => {
    vi.mocked(householdsApi.updateHousehold).mockResolvedValue({
      ...household,
      country: "CA",
      region: "CA-ON",
    });

    renderForm(true, { ...household, country: null, region: null });
    const user = userEvent.setup();

    await user.click(await screen.findByRole("combobox", { name: "Country" }));
    await user.click(await screen.findByRole("option", { name: "Canada" }));
    await user.click(screen.getByRole("combobox", { name: "Region" }));
    await user.click(await screen.findByRole("option", { name: "Ontario" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(householdsApi.updateHousehold).toHaveBeenCalledWith(
        "house-1",
        expect.objectContaining({ country: "CA", region: "CA-ON" })
      )
    );
  });

  it("clears an existing location", async () => {
    vi.mocked(householdsApi.updateHousehold).mockResolvedValue({
      ...household,
      country: null,
      region: null,
    });

    renderForm(true);
    const user = userEvent.setup();

    await user.click(await screen.findByRole("combobox", { name: "Country" }));
    await user.click(await screen.findByRole("option", { name: "Not set" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(householdsApi.updateHousehold).toHaveBeenCalledWith(
        "house-1",
        expect.objectContaining({ country: null, region: null })
      )
    );
  });

  it("resets the region when the country changes", async () => {
    renderForm(true);
    const user = userEvent.setup();

    expect(await screen.findByRole("combobox", { name: "Region" })).toHaveTextContent("Wisconsin");

    await user.click(screen.getByRole("combobox", { name: "Country" }));
    await user.click(await screen.findByRole("option", { name: "Canada" }));

    expect(screen.getByRole("combobox", { name: "Region" })).toHaveTextContent("Not set");
  });

  it("disables editing for a non-Owner", async () => {
    renderForm(false);

    expect(await screen.findByRole("combobox", { name: "Country" })).toBeDisabled();
    expect(screen.queryByRole("button", { name: "Save" })).not.toBeInTheDocument();
  });
});
