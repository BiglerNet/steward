import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as maintenanceScheduleApi from "@/api/maintenanceSchedule";
import type { MaintenanceScheduleEntryResponse } from "@/api/types";
import { MaintenanceScheduleSection } from "@/components/maintenance/MaintenanceScheduleSection";

vi.mock("@/api/maintenanceSchedule");

function entry(overrides: Partial<MaintenanceScheduleEntryResponse> = {}): MaintenanceScheduleEntryResponse {
  return {
    templateId: "template-1",
    templateTitle: "Winterization",
    templateStepId: "step-1",
    stepText: "Change oil",
    engineId: null,
    engineLabel: null,
    lastDoneAt: "2026-06-01T00:00:00Z",
    lastDoneReading: { value: 4200, unit: "Miles" },
    intervalMonths: 3,
    intervalMiles: 3000,
    intervalHours: null,
    dueStatus: "DueSoon",
    ...overrides,
  };
}

function renderSection() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MaintenanceScheduleSection householdId="house-1" assetId="asset-1" />
    </QueryClientProvider>
  );
}

describe("MaintenanceScheduleSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows last-done date and reading alongside the due-status badge", async () => {
    vi.mocked(maintenanceScheduleApi.getMaintenanceSchedule).mockResolvedValue([entry()]);

    renderSection();

    expect(await screen.findByText("Change oil")).toBeInTheDocument();
    expect(screen.getByText(/Last done:.*4,200 mi/)).toBeInTheDocument();
    expect(screen.getByText("Due soon")).toBeInTheDocument();
  });

  it("lists divergent engines as separate rows", async () => {
    vi.mocked(maintenanceScheduleApi.getMaintenanceSchedule).mockResolvedValue([
      entry({ templateStepId: "step-1", engineId: "engine-main", engineLabel: "Main", lastDoneAt: "2026-01-01T00:00:00Z" }),
      entry({ templateStepId: "step-1", engineId: "engine-kicker", engineLabel: "Kicker", lastDoneAt: null, lastDoneReading: null }),
    ]);

    renderSection();

    expect(await screen.findByText("Change oil · Main")).toBeInTheDocument();
    expect(screen.getByText("Change oil · Kicker")).toBeInTheDocument();
  });

  it("shows Never for a step that has never been completed", async () => {
    vi.mocked(maintenanceScheduleApi.getMaintenanceSchedule).mockResolvedValue([
      entry({ lastDoneAt: null, lastDoneReading: null, dueStatus: "Overdue" }),
    ]);

    renderSection();

    expect(await screen.findByText("Last done: Never")).toBeInTheDocument();
  });

  it.each([
    ["Overdue", "Overdue"],
    ["DueSoon", "Due soon"],
    ["Upcoming", "Upcoming"],
    ["OK", "OK"],
    ["Unknown", "Unknown"],
  ] as const)("renders the %s due-status badge", async (status, label) => {
    vi.mocked(maintenanceScheduleApi.getMaintenanceSchedule).mockResolvedValue([entry({ dueStatus: status })]);

    renderSection();

    expect(await screen.findByText(label)).toBeInTheDocument();
  });
});
