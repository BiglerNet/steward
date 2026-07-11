import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as dashboardsApi from "@/api/dashboards";
import type {
  DashboardDetailResponse,
  DashboardSnapshot,
  DashboardSummaryResponse,
  HouseholdResponse,
} from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { DashboardPage } from "@/pages/DashboardPage";

vi.mock("@/api/dashboards");
vi.mock("@/hooks/useHouseholds");

const summary: DashboardSummaryResponse = {
  id: "dash-1",
  name: "Overview",
  isDefault: true,
  position: 0,
};

function detail(widgets: DashboardDetailResponse["widgets"]): DashboardDetailResponse {
  return { id: "dash-1", name: "Overview", isDefault: true, position: 0, widgets };
}

const snapshot: DashboardSnapshot = {};

function household(userRole: "Owner" | "Contributor" | "Viewer"): HouseholdResponse {
  return {
    id: "house-1",
    name: "Garage",
    publicSlug: "garage",
    isPublicVisible: false,
    country: null,
    region: null,
    userRole,
    storageUsedBytes: 0,
    storageQuotaBytes: 1073741824,
    createdAt: "2026-01-01T00:00:00Z",
  };
}

function mockRole(userRole: "Owner" | "Contributor" | "Viewer") {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [household(userRole)],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);
}

function renderPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/dashboards"]}>
        <Routes>
          <Route path="/households/:householdId/dashboards" element={<DashboardPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("DashboardPage edit mode", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(dashboardsApi.listDashboards).mockResolvedValue([summary]);
    vi.mocked(dashboardsApi.getDashboard).mockResolvedValue(
      detail([
        { id: "w1", widgetType: "CylinderIndex", widgetSize: "Small", position: 0, config: null },
        { id: "w2", widgetType: "TotalDisplacement", widgetSize: "Small", position: 1, config: null },
      ])
    );
    vi.mocked(dashboardsApi.getDashboardSnapshot).mockResolvedValue(snapshot);
  });

  it("hides the edit control for a Viewer", async () => {
    mockRole("Viewer");
    renderPage();

    await screen.findByText("Cylinder Index");
    expect(screen.queryByRole("button", { name: "Edit Dashboard" })).not.toBeInTheDocument();
  });

  it("entering edit mode shows drag/resize/remove controls on each widget", async () => {
    mockRole("Contributor");
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    expect(screen.getAllByLabelText("Drag to reorder widget")).toHaveLength(2);
    expect(screen.getAllByLabelText("Resize widget")).toHaveLength(2);
    expect(screen.getAllByLabelText("Remove widget")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Save Layout" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Cancel" })).toBeInTheDocument();
  });

  it("cycles a widget's size when its resize control is activated", async () => {
    mockRole("Contributor");
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    const w1 = document.querySelector('[data-widget-id="w1"]') as HTMLElement;
    const resizeButton = within(w1).getByLabelText("Resize widget");
    expect(resizeButton).toHaveAttribute("title", "Resize (Small → Wide)");

    await user.click(resizeButton);
    expect(within(w1).getByLabelText("Resize widget")).toHaveAttribute(
      "title",
      "Resize (Wide → Full)"
    );

    await user.click(within(w1).getByLabelText("Resize widget"));
    expect(within(w1).getByLabelText("Resize widget")).toHaveAttribute(
      "title",
      "Resize (Full → Small)"
    );
  });

  it("adds a widget from the catalog into the staged layout", async () => {
    mockRole("Contributor");
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));
    await user.click(screen.getByRole("button", { name: "Add Widget" }));
    await user.click(await screen.findByText("Total Horsepower"));

    expect(screen.getByText("Total Horsepower")).toBeInTheDocument();
  });

  it("removes a widget from the staged layout", async () => {
    mockRole("Contributor");
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    const w2 = document.querySelector('[data-widget-id="w2"]') as HTMLElement;
    await user.click(within(w2).getByLabelText("Remove widget"));

    expect(screen.queryByText("Total Displacement")).not.toBeInTheDocument();
    expect(screen.getByText("Cylinder Index")).toBeInTheDocument();
  });

  it("saves staged changes and exits edit mode", async () => {
    mockRole("Contributor");
    vi.mocked(dashboardsApi.replaceWidgetLayout).mockResolvedValue(
      detail([{ id: "w1", widgetType: "CylinderIndex", widgetSize: "Small", position: 0, config: null }])
    );
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    const w2 = document.querySelector('[data-widget-id="w2"]') as HTMLElement;
    await user.click(within(w2).getByLabelText("Remove widget"));
    await user.click(screen.getByRole("button", { name: "Save Layout" }));

    await waitFor(() =>
      expect(dashboardsApi.replaceWidgetLayout).toHaveBeenCalledWith(
        "house-1",
        "dash-1",
        expect.objectContaining({
          widgets: [{ widgetType: "CylinderIndex", widgetSize: "Small", config: undefined }],
        })
      )
    );
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Edit Dashboard" })).toBeInTheDocument()
    );
  });

  it("keeps edit mode active with staged changes preserved when save fails", async () => {
    mockRole("Contributor");
    vi.mocked(dashboardsApi.replaceWidgetLayout).mockRejectedValue(new Error("network error"));
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    const w2 = document.querySelector('[data-widget-id="w2"]') as HTMLElement;
    await user.click(within(w2).getByLabelText("Remove widget"));
    await user.click(screen.getByRole("button", { name: "Save Layout" }));

    await waitFor(() => expect(dashboardsApi.replaceWidgetLayout).toHaveBeenCalled());
    expect(screen.getByRole("button", { name: "Save Layout" })).toBeInTheDocument();
    expect(screen.queryByText("Total Displacement")).not.toBeInTheDocument();
  });

  it("discards staged changes on cancel", async () => {
    mockRole("Contributor");
    renderPage();
    const user = userEvent.setup();

    await screen.findByText("Cylinder Index");
    await user.click(screen.getByRole("button", { name: "Edit Dashboard" }));

    const w2 = document.querySelector('[data-widget-id="w2"]') as HTMLElement;
    await user.click(within(w2).getByLabelText("Remove widget"));
    expect(screen.queryByText("Total Displacement")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(dashboardsApi.replaceWidgetLayout).not.toHaveBeenCalled();
    expect(screen.getByText("Total Displacement")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Edit Dashboard" })).toBeInTheDocument();
  });
});
