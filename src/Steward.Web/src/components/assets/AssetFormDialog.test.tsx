import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetsApi from "@/api/assets";
import * as assetTypesApi from "@/api/assetTypes";
import type { AssetResponse } from "@/api/types";
import { AssetFormDialog } from "@/components/assets/AssetFormDialog";
import { Button } from "@/components/ui/button";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";

vi.mock("@/api/assets");
vi.mock("@/api/assetTypes");

const carAsset: AssetResponse = {
  id: "asset-1",
  householdId: "house-1",
  category: "Car",
  structuralType: "Vehicle",
  name: "Daily Driver",
  description: null,
  year: 2020,
  coverPhotoId: null,
  usageTrackingMode: "Mileage",
  vin: "1HGCM82633A004352",
  make: "Honda",
  model: "Accord",
  color: "Blue",
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
  licensePlate: "ABC-123",
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
  powertrain: null,
};

const sailboatAsset: AssetResponse = {
  ...carAsset,
  category: "Sailboat",
  structuralType: "Boat",
  name: "Wind Dancer",
  usageTrackingMode: "Hours",
  vin: null,
  make: "Catalina",
  model: "22",
  color: null,
  licensePlate: null,
  hin: "XYZ98765E505",
  hullMaterial: "Fiberglass",
  hullType: "Monohull",
  keelType: "Fin",
  mastHeightFt: 42,
  mastCount: 1,
  lengthFt: 22,
  beamFt: 8,
};

const powerBoatAsset: AssetResponse = {
  ...carAsset,
  category: "PowerBoat",
  structuralType: "Boat",
  name: "Sea Ray",
  usageTrackingMode: "Both",
  vin: null,
  make: "Sea Ray",
  model: "Sundancer",
  color: null,
  licensePlate: null,
  hin: "ABC12345D404",
  hullMaterial: "Fiberglass",
  hullType: "Monohull",
  driveType: "SternDrive",
  lengthFt: 24.5,
  beamFt: 8.5,
};

const pwcAsset: AssetResponse = {
  ...carAsset,
  category: "Pwc",
  structuralType: "Boat",
  name: "Jet Ski",
  usageTrackingMode: "Hours",
  vin: null,
  make: "Sea-Doo",
  model: "GTI",
  color: null,
  licensePlate: null,
  hin: "DEF45678G606",
  hullMaterial: "Fiberglass",
  lengthFt: 10,
  beamFt: 4,
};

function renderDialog(asset: AssetResponse = carAsset) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <AssetFormDialog householdId="house-1" asset={asset} trigger={<Button>Edit</Button>} />
    </QueryClientProvider>
  );
}

describe("AssetFormDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(assetTypesApi.listAssetTypes).mockResolvedValue(testAssetTypeRegistry);
  });

  it("shows the registry-driven fields for the asset's category, prefilled", async () => {
    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));

    expect(await screen.findByLabelText("VIN")).toHaveValue("1HGCM82633A004352");
    expect(screen.getByLabelText("Name")).toHaveValue("Daily Driver");
    expect(screen.queryByLabelText("Cutting width (in)")).not.toBeInTheDocument();
  });

  it("displays the category as read-only, not a selector", async () => {
    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));

    await screen.findByLabelText("VIN");
    expect(screen.getByText("Car")).toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Category" })).not.toBeInTheDocument();
  });

  it("submits an update request with the edited fields", async () => {
    vi.mocked(assetsApi.updateAsset).mockResolvedValue({ ...carAsset, name: "Updated Name" });

    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));
    const nameInput = await screen.findByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Updated Name");

    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => expect(assetsApi.updateAsset).toHaveBeenCalled());
    const [, , payload] = vi.mocked(assetsApi.updateAsset).mock.calls[0];
    expect(payload).toMatchObject({ name: "Updated Name", vin: "1HGCM82633A004352", cuttingWidthIn: null });
    expect(assetsApi.createAsset).not.toHaveBeenCalled();
  });

  it("requires a name before submitting", async () => {
    renderDialog();
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));
    const nameInput = await screen.findByLabelText("Name");
    await user.clear(nameInput);
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Name is required")).toBeInTheDocument();
    expect(assetsApi.updateAsset).not.toHaveBeenCalled();
  });

  it("renders exactly the Sailboat registry fields, with selects for hullType and no driveType", async () => {
    renderDialog(sailboatAsset);
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));

    expect(await screen.findByLabelText("HIN")).toHaveValue("XYZ98765E505");
    expect(screen.getByLabelText("Hull material")).toHaveValue("Fiberglass");
    expect(screen.getByRole("combobox", { name: "Hull type" })).toHaveTextContent("Monohull");
    expect(screen.getByLabelText("Keel type")).toHaveValue("Fin");
    expect(screen.getByLabelText("Mast height (ft)")).toHaveValue(42);
    expect(screen.getByLabelText("Mast count")).toHaveValue(1);
    expect(screen.getByLabelText("Length (ft)")).toHaveValue(22);
    expect(screen.getByLabelText("Beam (ft)")).toHaveValue(8);
    expect(screen.getByLabelText("Make")).toHaveValue("Catalina");
    expect(screen.getByLabelText("Model")).toHaveValue("22");
    expect(screen.getByLabelText("Color")).toHaveValue("");
    expect(screen.queryByRole("combobox", { name: "Drive type" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("VIN")).not.toBeInTheDocument();
  });

  it("renders drive type as a select for a PowerBoat, with no keel/mast fields", async () => {
    renderDialog(powerBoatAsset);
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));

    await screen.findByLabelText("HIN");
    expect(screen.getByRole("combobox", { name: "Hull type" })).toHaveTextContent("Monohull");
    expect(screen.getByRole("combobox", { name: "Drive type" })).toHaveTextContent("Stern drive (I/O)");
    expect(screen.queryByLabelText("Keel type")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Mast height (ft)")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Mast count")).not.toBeInTheDocument();
  });

  it("leaves the Pwc field list unchanged (no hull/drive type, keel, or mast fields)", async () => {
    renderDialog(pwcAsset);
    const user = userEvent.setup();

    await user.click(screen.getByText("Edit"));

    expect(await screen.findByLabelText("HIN")).toHaveValue("DEF45678G606");
    expect(screen.getByLabelText("Hull material")).toHaveValue("Fiberglass");
    expect(screen.getByLabelText("Length (ft)")).toHaveValue(10);
    expect(screen.getByLabelText("Beam (ft)")).toHaveValue(4);
    expect(screen.queryByRole("combobox", { name: "Hull type" })).not.toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: "Drive type" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Keel type")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Mast height (ft)")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Mast count")).not.toBeInTheDocument();
  });
});
