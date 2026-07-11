import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as householdsApi from "@/api/households";
import { Button } from "@/components/ui/button";
import { CreateHouseholdDialog } from "@/components/households/CreateHouseholdDialog";

vi.mock("@/api/households");

function CurrentPath() {
  const location = useLocation();
  return <span data-testid="path">{location.pathname}</span>;
}

function renderDialog() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households"]}>
        <Routes>
          <Route
            path="/households"
            element={
              <>
                <CreateHouseholdDialog trigger={<Button>Open create dialog</Button>} />
                <CurrentPath />
              </>
            }
          />
          <Route path="/households/:id" element={<CurrentPath />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("CreateHouseholdDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("creates a household and navigates into it", async () => {
    vi.mocked(householdsApi.createHousehold).mockResolvedValue({
      id: "house-1",
      name: "Smith Garage",
      publicSlug: "smith-garage-abc123",
      isPublicVisible: false,
      country: null,
      region: null,
      userRole: "Owner",
      storageUsedBytes: 0,
      storageQuotaBytes: 1073741824,
      createdAt: "2026-01-01T00:00:00Z",
    });

    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Open create dialog"));
    await user.type(screen.getByLabelText("Household name"), "Smith Garage");
    await user.click(screen.getByRole("button", { name: "Create household" }));

    expect(await screen.findByTestId("path")).toHaveTextContent("/households/house-1");
    expect(householdsApi.createHousehold).toHaveBeenCalledWith(
      expect.objectContaining({ name: "Smith Garage", isPublicVisible: false })
    );
  });
});
