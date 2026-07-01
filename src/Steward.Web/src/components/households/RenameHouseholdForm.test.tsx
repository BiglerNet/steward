import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as householdsApi from "@/api/households";
import type { HouseholdResponse } from "@/api/types";
import { RenameHouseholdForm } from "@/components/households/RenameHouseholdForm";

vi.mock("@/api/households");

const household: HouseholdResponse = {
  id: "house-1",
  name: "Smith Garage",
  publicSlug: "smith-garage",
  isPublicVisible: false,
  userRole: "Owner",
  createdAt: "2026-01-01T00:00:00Z",
};

function renderForm(canEdit: boolean) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <RenameHouseholdForm household={household} canEdit={canEdit} />
    </QueryClientProvider>
  );
}

describe("RenameHouseholdForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renames the household", async () => {
    vi.mocked(householdsApi.updateHousehold).mockResolvedValue({
      ...household,
      name: "Lake Garage",
    });

    renderForm(true);
    const user = userEvent.setup();

    await user.clear(screen.getByLabelText("Household name"));
    await user.type(screen.getByLabelText("Household name"), "Lake Garage");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(householdsApi.updateHousehold).toHaveBeenCalledWith(
        "house-1",
        expect.objectContaining({ name: "Lake Garage" })
      )
    );
  });

  it("disables editing for a Viewer", () => {
    renderForm(false);

    expect(screen.getByLabelText("Household name")).toBeDisabled();
    expect(screen.queryByRole("button", { name: "Save" })).not.toBeInTheDocument();
  });
});
