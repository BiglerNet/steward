import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import { AssetFormDialog } from "@/components/assets/AssetFormDialog";
import { Button } from "@/components/ui/button";

vi.mock("@/api/assets");

function CurrentPath() {
  const location = useLocation();
  return <span data-testid="path">{location.pathname}</span>;
}

function renderDialog() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets"
            element={
              <>
                <AssetFormDialog
                  householdId="house-1"
                  trigger={<Button>Add asset</Button>}
                />
                <CurrentPath />
              </>
            }
          />
          <Route path="/households/:householdId/assets/:assetId" element={<CurrentPath />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AssetFormDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows vehicle fields for the default Car type and switches to type-specific fields", async () => {
    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Add asset"));

    expect(screen.getByLabelText("VIN")).toBeInTheDocument();
    expect(screen.queryByLabelText("Cutting width (in)")).not.toBeInTheDocument();

    await user.click(screen.getByRole("combobox", { name: "Asset type" }));
    await user.click(await screen.findByRole("option", { name: "Riding Mower" }));

    expect(screen.queryByLabelText("VIN")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Cutting width (in)")).toBeInTheDocument();
  });

  it("clears inapplicable type-specific fields when the asset type changes before submit", async () => {
    vi.mocked(assetsApi.createAsset).mockResolvedValue({
      id: "asset-1",
      householdId: "house-1",
      assetType: "RidingMower",
      name: "Mower",
      description: null,
      year: null,
      photoUrl: null,
      usageTrackingMode: "None",
      vin: null,
      color: null,
      make: null,
      model: null,
      hin: null,
      hullMaterial: null,
      lengthFt: null,
      beamFt: null,
      trackLengthIn: null,
      ballSizeIn: null,
      maxLoadLbs: null,
      interiorHeightFt: null,
      interiorLengthFt: null,
      cuttingWidthIn: 42,
      maxPsi: null,
      maxGpm: null,
      equipmentDescription: null,
      createdAt: "2026-01-01T00:00:00Z",
      updatedAt: "2026-01-01T00:00:00Z",
    });

    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Add asset"));
    await user.type(screen.getByLabelText("Name"), "Mower");
    await user.type(screen.getByLabelText("VIN"), "should-not-be-sent");

    await user.click(screen.getByRole("combobox", { name: "Asset type" }));
    await user.click(await screen.findByRole("option", { name: "Riding Mower" }));
    await user.type(screen.getByLabelText("Cutting width (in)"), "42");

    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(assetsApi.createAsset).toHaveBeenCalled());
    const [, payload] = vi.mocked(assetsApi.createAsset).mock.calls[0];
    expect(payload).toMatchObject({ assetType: "RidingMower", vin: null, cuttingWidthIn: 42 });

    expect(await screen.findByTestId("path")).toHaveTextContent("/households/house-1/assets/asset-1");
  });

  it("requires a name before submitting", async () => {
    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Add asset"));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Name is required")).toBeInTheDocument();
    expect(assetsApi.createAsset).not.toHaveBeenCalled();
  });
});
