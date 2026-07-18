import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as maintenanceItemsApi from "@/api/maintenanceItems";
import * as templatesApi from "@/api/templates";
import type { MaintenanceItemResponse, TemplateResponse } from "@/api/types";
import { QuickCreateMaintenanceItemDialog } from "@/components/maintenance/QuickCreateMaintenanceItemDialog";

vi.mock("@/api/maintenanceItems");
vi.mock("@/api/templates");

function template(overrides: Partial<TemplateResponse> = {}): TemplateResponse {
  return {
    id: "template-1",
    householdId: null,
    title: "Oil change",
    description: null,
    applicableCategories: [],
    steps: [{ id: "step-1", templateId: "template-1", text: "Drain oil", sortOrder: 0, engineScoped: false, recurrenceIntervalMonths: null, recurrenceIntervalMiles: null, recurrenceIntervalHours: null, suggestedParts: [] }],
    ...overrides,
  };
}

function createdItem(overrides: Partial<MaintenanceItemResponse> = {}): MaintenanceItemResponse {
  return {
    id: "item-1",
    assetId: "asset-1",
    engineId: null,
    templateId: null,
    title: "Oil change",
    description: null,
    providerName: null,
    status: "Planned",
    date: null,
    cost: null,
    odometerMiles: null,
    engineHours: null,
    isBlocked: false,
    completedAt: null,
    checklistItems: [],
    partLines: [],
    ...overrides,
  };
}

function renderDialog() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/assets/asset-1/maintenance"]}>
        <Routes>
          <Route
            path="/households/:householdId/assets/:assetId/maintenance"
            element={<QuickCreateMaintenanceItemDialog householdId="house-1" assetId="asset-1" assetCategory="Car" />}
          />
          <Route
            path="/households/:householdId/assets/:assetId/maintenance/:itemId"
            element={<p>Editor page</p>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("QuickCreateMaintenanceItemDialog", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([]);
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([]);
  });

  it("creates an item without a template and navigates to its editor", async () => {
    vi.mocked(maintenanceItemsApi.createMaintenanceItem).mockResolvedValue(createdItem());
    const user = userEvent.setup();

    renderDialog();
    await user.click(screen.getByRole("button", { name: "New" }));
    await user.type(screen.getByLabelText("Title"), "Oil change");
    await user.click(screen.getByRole("button", { name: "Create" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.createMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ title: "Oil change" })
      )
    );
    expect(await screen.findByText("Editor page")).toBeInTheDocument();
  });

  it("shows a step preview when a template is picked", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([template()]);
    const user = userEvent.setup();

    renderDialog();
    await user.click(screen.getByRole("button", { name: "New" }));

    await user.click(await screen.findByText("Oil change"));
    expect(await screen.findByText("Drain oil")).toBeInTheDocument();
  });

  it("creates from a picked template", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([template()]);
    vi.mocked(maintenanceItemsApi.createMaintenanceItem).mockResolvedValue(
      createdItem({ templateId: "template-1" })
    );
    const user = userEvent.setup();

    renderDialog();
    await user.click(screen.getByRole("button", { name: "New" }));
    await user.type(screen.getByLabelText("Title"), "Winterize");
    await user.click(await screen.findByText("Oil change"));
    await user.click(screen.getByRole("button", { name: "Create" }));

    await waitFor(() =>
      expect(maintenanceItemsApi.createMaintenanceItem).toHaveBeenCalledWith(
        "house-1",
        "asset-1",
        expect.objectContaining({ templateId: "template-1" })
      )
    );
  });
});
