import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as templatesApi from "@/api/templates";
import type { TemplateResponse } from "@/api/types";
import { TemplatePicker } from "@/components/templates/TemplatePicker";

vi.mock("@/api/templates");

function template(overrides: Partial<TemplateResponse> = {}): TemplateResponse {
  return {
    id: "t-1",
    householdId: null,
    title: "Oil change",
    description: null,
    applicableCategories: [],
    steps: [],
    ...overrides,
  };
}

function renderPicker(assetCategory: TemplateResponse["applicableCategories"][number] = "Snowmobile") {
  const queryClient = new QueryClient();
  const onChange = vi.fn();
  render(
    <QueryClientProvider client={queryClient}>
      <TemplatePicker householdId="house-1" assetCategory={assetCategory} value={null} onChange={onChange} />
    </QueryClientProvider>
  );
  return onChange;
}

describe("TemplatePicker", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(templatesApi.listHouseholdTemplates).mockResolvedValue([]);
  });

  it("lists a template applicable to every category (empty applicableCategories)", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([template({ title: "Oil change" })]);
    renderPicker("Snowmobile");
    expect(await screen.findByText("Oil change")).toBeInTheDocument();
  });

  it("excludes a template whose applicableCategories doesn't include the asset's category", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([
      template({ title: "Boat winterize", applicableCategories: ["PowerBoat", "Sailboat"] }),
    ]);
    renderPicker("Snowmobile");

    await screen.findByPlaceholderText("Search templates…");
    expect(screen.queryByText("Boat winterize")).not.toBeInTheDocument();
  });

  it("includes a template whose applicableCategories includes the asset's category", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([
      template({ title: "Track lube", applicableCategories: ["Snowmobile"] }),
    ]);
    renderPicker("Snowmobile");
    expect(await screen.findByText("Track lube")).toBeInTheDocument();
  });

  it("calls onChange with the template id when selected", async () => {
    vi.mocked(templatesApi.listPlatformTemplates).mockResolvedValue([template({ title: "Oil change" })]);
    const user = userEvent.setup();
    const onChange = renderPicker("Car");

    await user.click(await screen.findByText("Oil change"));
    expect(onChange).toHaveBeenCalledWith("t-1", expect.objectContaining({ id: "t-1", title: "Oil change" }));
  });
});
