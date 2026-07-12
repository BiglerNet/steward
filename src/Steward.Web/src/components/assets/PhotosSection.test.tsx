import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AxiosError } from "axios";
import { MemoryRouter, Route, Routes } from "react-router";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as assetPhotosApi from "@/api/assetPhotos";
import * as documentsApi from "@/api/documents";
import type { AssetPhotoResponse, AssetResponse, HouseholdResponse } from "@/api/types";
import { PhotosSection } from "@/components/assets/PhotosSection";
import * as useHouseholdsModule from "@/hooks/useHouseholds";

vi.mock("@/api/assetPhotos", async (importOriginal) => {
  const actual = await importOriginal<typeof assetPhotosApi>();
  return {
    ...actual,
    listAssetPhotos: vi.fn(),
    uploadAssetPhoto: vi.fn(),
    deleteAssetPhoto: vi.fn(),
    setCoverPhoto: vi.fn(),
  };
});
vi.mock("@/api/documents");
vi.mock("@/hooks/useHouseholds");
vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

const asset: AssetResponse = {
  id: "asset-1",
  householdId: "house-1",
  category: "Snowmobile",
  structuralType: "Vehicle",
  name: "Trail Blazer",
  description: null,
  year: 2020,
  coverPhotoId: "photo-1",
  usageTrackingMode: "Both",
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
  powertrain: null,
};

const photo1: AssetPhotoResponse = {
  id: "photo-1",
  assetId: "asset-1",
  width: 2048,
  height: 1536,
  sizeBytes: 900_000,
  createdAt: "2026-01-02T00:00:00Z",
};

const photo2: AssetPhotoResponse = {
  id: "photo-2",
  assetId: "asset-1",
  width: 2048,
  height: 1536,
  sizeBytes: 850_000,
  createdAt: "2026-01-01T00:00:00Z",
};

function household(overrides: Partial<HouseholdResponse> = {}): HouseholdResponse {
  return {
    id: "house-1",
    name: "Garage",
    publicSlug: "garage",
    isPublicVisible: false,
    country: null,
    region: null,
    userRole: "Contributor",
    storageUsedBytes: 0,
    storageQuotaBytes: 1_073_741_824,
    createdAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function mockRole(userRole: "Owner" | "Contributor" | "Viewer") {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [household({ userRole })],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
}

function renderSection(assetOverrides: Partial<AssetResponse> = {}) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId"
            element={<PhotosSection asset={{ ...asset, ...assetOverrides }} />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("PhotosSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(documentsApi.downloadDocument).mockResolvedValue(new Blob(["fake"], { type: "image/jpeg" }));
  });

  it("renders the gallery with a cover marker on the cover photo", async () => {
    mockRole("Contributor");
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([photo1, photo2]);

    renderSection();

    await waitFor(() => expect(screen.getAllByRole("img")).toHaveLength(2));
    expect(screen.getByText("Cover")).toBeInTheDocument();
  });

  it("shows an empty state with no photos", async () => {
    mockRole("Contributor");
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([]);

    renderSection();

    expect(await screen.findByText(/No photos yet/)).toBeInTheDocument();
  });

  it("hides upload, delete, and set-cover controls for a Viewer", async () => {
    mockRole("Viewer");
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([photo1, photo2]);

    renderSection();

    await waitFor(() => expect(screen.getAllByRole("img")).toHaveLength(2));
    expect(screen.queryByRole("button", { name: "Add photo" })).not.toBeInTheDocument();
    expect(screen.queryByText("Delete")).not.toBeInTheDocument();
    expect(screen.queryByText("Set cover")).not.toBeInTheDocument();
  });

  it("uploads a photo and refreshes the gallery", async () => {
    mockRole("Contributor");
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([]);
    vi.mocked(assetPhotosApi.uploadAssetPhoto).mockResolvedValue(photo1);

    renderSection();
    const user = userEvent.setup();

    await screen.findByText(/No photos yet/);

    const file = new File(["content"], "photo.jpg", { type: "image/jpeg" });
    const input = screen.getByLabelText("Photo file");
    await user.upload(input, file);

    await waitFor(() =>
      expect(assetPhotosApi.uploadAssetPhoto).toHaveBeenCalledWith("house-1", "asset-1", file)
    );
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith("Photo uploaded."));
  });

  it("surfaces a quota-exceeded rejection via toast", async () => {
    mockRole("Contributor");
    vi.mocked(assetPhotosApi.listAssetPhotos).mockResolvedValue([]);
    const axiosError = new AxiosError("Bad Request");
    axiosError.response = {
      status: 400,
      data: { title: "This upload would exceed the household's storage quota." },
      statusText: "Bad Request",
      headers: {},
      config: {} as never,
    };
    vi.mocked(assetPhotosApi.uploadAssetPhoto).mockRejectedValue(axiosError);

    renderSection();
    const user = userEvent.setup();

    await screen.findByText(/No photos yet/);

    const file = new File(["content"], "photo.jpg", { type: "image/jpeg" });
    const input = screen.getByLabelText("Photo file");
    await user.upload(input, file);

    await waitFor(() =>
      expect(toast.error).toHaveBeenCalledWith("This upload would exceed the household's storage quota.")
    );
  });
});
