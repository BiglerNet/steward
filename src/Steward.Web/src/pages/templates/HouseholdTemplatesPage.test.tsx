import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as templatesApi from "@/api/templates";
import type { TemplateResponse } from "@/api/types";
import * as useHouseholdsModule from "@/hooks/useHouseholds";
import { HouseholdTemplatesSection } from "@/pages/templates/HouseholdTemplatesPage";

vi.mock("@/api/templates");
vi.mock("@/hooks/useHouseholds");

function template(overrides: Partial<TemplateResponse> = {}): TemplateResponse {
  return {
    id: "t-1",
    householdId: "house-1",
    title: "Spring commissioning",
    description: null,
    applicableCategories: [],
    steps: [],
    ...overrides,
  };
}

function renderPage() {
  vi.mocked(useHouseholdsModule.useHouseholds).mockReturnValue({
    data: [
      {
        id: "house-1",
        name: "Garage",
        publicSlug: "garage",
        isPublicVisible: false,
        country: null,
        region: null,
        userRole: "Contributor",
        storageUsedBytes: 0,
        storageQuotaBytes: 0,
        createdAt: "2026-01-01T00:00:00Z",
      },
    ],
  } as ReturnType<typeof useHouseholdsModule.useHouseholds>);

  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/households/house-1/settings"]}>
        <Routes>
          <Route
            path="/households/:householdId/settings"
            element={<HouseholdTemplatesSection householdId="house-1" />}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe("HouseholdTemplatesSection", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([]);
  });

  it("creates a new household template", async () => {
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([]);
    vi.mocked(templatesApi.createHouseholdTemplate).mockResolvedValue(template());
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByRole("button", { name: "New template" }));
    await user.type(screen.getByLabelText("Title"), "Spring commissioning");
    await user.click(screen.getByRole("button", { name: "Create" }));

    await waitFor(() =>
      expect(templatesApi.createHouseholdTemplate).toHaveBeenCalledWith(
        "house-1",
        expect.objectContaining({ title: "Spring commissioning" })
      )
    );
  });

  it("selects a template and adds a step to it", async () => {
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([template()]);
    vi.mocked(templatesApi.createHouseholdTemplateStep).mockResolvedValue({
      id: "step-1",
      templateId: "t-1",
      text: "Check belt",
      sortOrder: 0,
      engineScoped: false,
      recurrenceIntervalMonths: null,
      recurrenceIntervalMiles: null,
      recurrenceIntervalHours: null,
      suggestedParts: [],
    });
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByText("Spring commissioning"));
    await user.click(await screen.findByRole("button", { name: "Add step" }));
    await user.type(screen.getByLabelText("Step"), "Check belt");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(templatesApi.createHouseholdTemplateStep).toHaveBeenCalledWith(
        "house-1",
        "t-1",
        expect.objectContaining({ text: "Check belt" })
      )
    );
  });

  it("reorders steps via the move-down control", async () => {
    const stepA = {
      id: "step-a",
      templateId: "t-1",
      text: "First",
      sortOrder: 0,
      engineScoped: false,
      recurrenceIntervalMonths: null,
      recurrenceIntervalMiles: null,
      recurrenceIntervalHours: null,
      suggestedParts: [],
    };
    const stepB = { ...stepA, id: "step-b", text: "Second", sortOrder: 1 };
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([
      template({ steps: [stepA, stepB] }),
    ]);
    vi.mocked(templatesApi.reorderHouseholdTemplateSteps).mockResolvedValue([stepB, stepA]);
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByText("Spring commissioning"));
    await user.click(await screen.findAllByRole("button", { name: "Move down" }).then((els) => els[0]));

    await waitFor(() =>
      expect(templatesApi.reorderHouseholdTemplateSteps).toHaveBeenCalledWith("house-1", "t-1", [
        "step-b",
        "step-a",
      ])
    );
  });

  it("duplicates a platform template into the household", async () => {
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([]);
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([
      template({ id: "platform-1", householdId: null, title: "Oil change" }),
    ]);
    vi.mocked(templatesApi.duplicateTemplate).mockResolvedValue(
      template({ id: "copy-1", title: "Oil change" })
    );
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByRole("button", { name: "Copy to my library to modify" }));

    await waitFor(() =>
      expect(templatesApi.duplicateTemplate).toHaveBeenCalledWith("house-1", {
        platformTemplateId: "platform-1",
      })
    );
  });
});
