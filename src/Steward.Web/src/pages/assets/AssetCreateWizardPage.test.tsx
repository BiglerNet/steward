import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetPhotosApi from "@/api/assetPhotos";
import * as assetsApi from "@/api/assets";
import * as assetTypesApi from "@/api/assetTypes";
import * as enginesApi from "@/api/engines";
import type { AssetResponse, EngineResponse, VinDecodeResult } from "@/api/types";
import * as vinDecodeApi from "@/api/vinDecode";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { AssetCreateWizardPage } from "@/pages/assets/AssetCreateWizardPage";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";

vi.mock("@/api/assets");
vi.mock("@/api/assetTypes");
vi.mock("@/api/vinDecode");
vi.mock("@/api/engines");
vi.mock("@/api/assetPhotos");
vi.mock("@/hooks/useHouseholds");

function mockRole(userRole: "Owner" | "Contributor" | "Viewer") {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    isLoading: false,
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

function makeAsset(overrides: Partial<AssetResponse> = {}): AssetResponse {
  return {
    id: "new-asset-1",
    householdId: "house-1",
    category: "Car",
    structuralType: "Vehicle",
    name: "Test Car",
    description: null,
    year: null,
    coverPhotoId: null,
    usageTrackingMode: "Mileage",
    vin: null,
    make: null,
    model: null,
    color: null,
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
    licensePlate: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function makeEngine(overrides: Partial<EngineResponse> = {}): EngineResponse {
  return {
    id: "engine-1",
    assetId: "new-asset-1",
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
    ...overrides,
  };
}

function renderWizard() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/new"]}>
        <Routes>
          <Route path="/households/:householdId/assets/new" element={<AssetCreateWizardPage />} />
          <Route path="/households/:householdId/assets" element={<div>Asset list page</div>} />
          <Route
            path="/households/:householdId/assets/:assetId"
            element={<div>Asset detail page</div>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("AssetCreateWizardPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(assetTypesApi.listAssetTypes).mockResolvedValue(testAssetTypeRegistry);
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([]);
  });

  it("redirects a Viewer to the asset list", async () => {
    mockRole("Viewer");

    renderWizard();

    expect(await screen.findByText("Asset list page")).toBeInTheDocument();
  });

  it("offers Car the full Type → VIN → Details → Engine → Photos flow", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.createAsset).mockResolvedValue(makeAsset());
    vi.mocked(enginesApi.createEngine).mockResolvedValue(makeEngine());
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByLabelText("VIN")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("Name"), "Test Car");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("Label"), "Main engine");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await waitFor(() => expect(assetsApi.createAsset).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(enginesApi.createEngine).toHaveBeenCalledTimes(1));
    expect(await screen.findByRole("button", { name: "Finish" })).toBeInTheDocument();
  });

  it("skips VIN and Engine steps for a trailer with no engine and no VIN support", async () => {
    mockRole("Contributor");
    vi.mocked(assetsApi.createAsset).mockResolvedValue(
      makeAsset({ category: "UtilityTrailer", structuralType: "Trailer", name: "Test Trailer" })
    );
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Utility Trailer$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    // Straight to Details — no VIN field, no VIN step.
    expect(await screen.findByLabelText("Name")).toBeInTheDocument();
    expect(screen.queryByLabelText("VIN")).not.toBeInTheDocument();

    await user.type(screen.getByLabelText("Name"), "Test Trailer");
    await user.click(screen.getByRole("button", { name: "Create asset" }));

    await waitFor(() => expect(assetsApi.createAsset).toHaveBeenCalledTimes(1));
    expect(enginesApi.createEngine).not.toHaveBeenCalled();
    expect(await screen.findByRole("button", { name: "Finish" })).toBeInTheDocument();
  });

  it("clears the VIN field when switching to a category without VIN support", async () => {
    mockRole("Contributor");
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Continue" })); // skip VIN decode

    await user.type(await screen.findByLabelText("VIN"), "1HGCM82633A004352");
    expect(screen.getByLabelText("VIN")).toHaveValue("1HGCM82633A004352");

    // Back to VIN step, then back to Type step.
    await user.click(screen.getByRole("button", { name: "Back" }));
    await user.click(screen.getByRole("button", { name: "Back" }));

    // Riding Mower has no vin in its applicable fields and no VIN decode support.
    await user.click(screen.getByRole("radio", { name: /^Riding Mower$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByLabelText("Cutting width (in)")).toBeInTheDocument();
    expect(screen.queryByLabelText("VIN")).not.toBeInTheDocument();

    // Switch back to Car — the VIN field should now be empty again.
    await user.click(screen.getByRole("button", { name: "Back" }));
    await user.click(screen.getByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Continue" })); // skip VIN decode

    expect(await screen.findByLabelText("VIN")).toHaveValue("");
  });

  it("prefills Details and Engine fields from a successful VIN decode, and submits manually entered horsepower/torque alongside them", async () => {
    mockRole("Contributor");
    const decodeResult: VinDecodeResult = {
      vin: "1HGCM82633A004352",
      make: "HONDA",
      model: "Accord",
      modelYear: 2003,
      bodyClass: "Sedan/Saloon",
      vehicleType: "PASSENGER CAR",
      fuelTypePrimary: "Gasoline",
      engineCylinders: 4,
      displacementLiters: 2.4,
    };
    vi.mocked(vinDecodeApi.decodeVin).mockResolvedValue(decodeResult);
    vi.mocked(assetsApi.createAsset).mockResolvedValue(makeAsset());
    vi.mocked(enginesApi.createEngine).mockResolvedValue(makeEngine());
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("VIN"), "1HGCM82633A004352");
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await waitFor(() => expect(vinDecodeApi.decodeVin).toHaveBeenCalledWith("1HGCM82633A004352"));

    expect(await screen.findByLabelText("Make")).toHaveValue("HONDA");
    expect(screen.getByLabelText("Model")).toHaveValue("Accord");
    expect(screen.getByLabelText("Year")).toHaveValue(2003);
    expect(screen.getByText(/Found: 2003 HONDA Accord/)).toBeInTheDocument();

    await user.type(screen.getByLabelText("Name"), "Test Car");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByLabelText("Cylinders")).toHaveValue(4);
    expect(screen.getByLabelText("Displacement (cc)")).toHaveValue(2400);

    await user.type(screen.getByLabelText("Label"), "Main engine");
    await user.type(screen.getByLabelText("HP"), "355");
    await user.type(screen.getByLabelText("Torque (ft-lbs)"), "350");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await waitFor(() => expect(enginesApi.createEngine).toHaveBeenCalledTimes(1));
    const payload = vi.mocked(enginesApi.createEngine).mock.calls[0][2];
    expect(payload.cylinders).toBe(4);
    expect(payload.displacementCc).toBe(2400);
    expect(payload.horsepowerHp).toBe(355);
    expect(payload.torqueNm).toBeCloseTo(474.47, 1);
  });

  it("shows a couldn't-decode notice and still advances to Details when decode fails (502)", async () => {
    mockRole("Contributor");
    vi.mocked(vinDecodeApi.decodeVin).mockRejectedValue(new Error("upstream failure"));
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("VIN"), "1HGCM82633A004352");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByLabelText("Name")).toBeInTheDocument();
    expect(screen.getByText(/Couldn't decode this VIN/)).toBeInTheDocument();
  });

  it("explains what the VIN unlocks", async () => {
    mockRole("Contributor");
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText(/prefill year, make, model, and engine specs/)).toBeInTheDocument();
  });

  it("blocks Continue with inline validation for a malformed VIN", async () => {
    mockRole("Contributor");
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("VIN"), "TOO-SHORT");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText(/valid 17-character VIN/)).toBeInTheDocument();
    expect(screen.queryByLabelText("Name")).not.toBeInTheDocument();
    expect(vinDecodeApi.decodeVin).not.toHaveBeenCalled();
  });

  it("advances past the VIN step via Skip even with a malformed VIN entered", async () => {
    mockRole("Contributor");
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("VIN"), "TOO-SHORT");
    await user.click(screen.getByRole("button", { name: "Skip" }));

    expect(await screen.findByLabelText("Name")).toBeInTheDocument();
    expect(vinDecodeApi.decodeVin).not.toHaveBeenCalled();
  });

  it("shows a soft mismatch hint without blocking when the decoded body class doesn't match", async () => {
    mockRole("Contributor");
    vi.mocked(vinDecodeApi.decodeVin).mockResolvedValue({
      vin: "1HGCM82633A004352",
      make: "Honda",
      model: null,
      modelYear: null,
      bodyClass: "Motorcycle",
      vehicleType: "MOTORCYCLE",
      fuelTypePrimary: null,
      engineCylinders: null,
      displacementLiters: null,
    });
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("VIN"), "1HGCM82633A004352");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText(/Decoded as Motorcycle/)).toBeInTheDocument();
    expect(await screen.findByLabelText("Name")).toBeInTheDocument();
  });

  it("keeps the created asset and offers retry/skip when engine creation fails", async () => {
    mockRole("Contributor");
    const asset = makeAsset();
    vi.mocked(assetsApi.createAsset).mockResolvedValue(asset);
    vi.mocked(enginesApi.createEngine)
      .mockRejectedValueOnce(new Error("engine create failed"))
      .mockResolvedValueOnce(makeEngine());
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Continue" })); // skip VIN decode

    await user.type(await screen.findByLabelText("Name"), "Test Car");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("Label"), "Main engine");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText(/Couldn't add this engine/)).toBeInTheDocument();
    expect(assetsApi.createAsset).toHaveBeenCalledTimes(1);

    await user.click(screen.getByRole("button", { name: "Retry" }));

    await waitFor(() => expect(enginesApi.createEngine).toHaveBeenCalledTimes(2));
    expect(assetsApi.createAsset).toHaveBeenCalledTimes(1);
    expect(await screen.findByRole("button", { name: "Finish" })).toBeInTheDocument();
  });

  it("keeps the created asset and skips the engine when the user chooses to skip after a failure", async () => {
    mockRole("Contributor");
    const asset = makeAsset();
    vi.mocked(assetsApi.createAsset).mockResolvedValue(asset);
    vi.mocked(enginesApi.createEngine).mockRejectedValue(new Error("engine create failed"));
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Car$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));
    await user.click(screen.getByRole("button", { name: "Continue" })); // skip VIN decode

    await user.type(await screen.findByLabelText("Name"), "Test Car");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("Label"), "Main engine");
    await user.click(screen.getByRole("button", { name: "Continue" }));

    expect(await screen.findByText(/Couldn't add this engine/)).toBeInTheDocument();

    const skipButtons = screen.getAllByRole("button", { name: "Skip" });
    await user.click(skipButtons[skipButtons.length - 1]);

    expect(await screen.findByRole("button", { name: "Finish" })).toBeInTheDocument();
    expect(assetsApi.createAsset).toHaveBeenCalledTimes(1);
  });

  it("navigates to the new asset's detail page when finishing", async () => {
    mockRole("Contributor");
    const asset = makeAsset({ category: "UtilityTrailer", structuralType: "Trailer" });
    vi.mocked(assetsApi.createAsset).mockResolvedValue(asset);
    const user = userEvent.setup();

    renderWizard();

    await user.click(await screen.findByRole("radio", { name: /^Utility Trailer$/ }));
    await user.click(screen.getByRole("button", { name: "Continue" }));

    await user.type(await screen.findByLabelText("Name"), "Test Trailer");
    await user.click(screen.getByRole("button", { name: "Create asset" }));

    await user.click(await screen.findByRole("button", { name: "Finish" }));

    expect(await screen.findByText("Asset detail page")).toBeInTheDocument();
  });
});
