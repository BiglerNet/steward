import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ChecklistSection } from "@/components/maintenance/ChecklistSection";
import type { ChecklistItemResponse } from "@/api/types";

function item(overrides: Partial<ChecklistItemResponse> = {}): ChecklistItemResponse {
  return {
    id: "item-1",
    maintenanceItemId: "mi-1",
    text: "Change oil",
    status: "Open",
    resolvedAt: null,
    sortOrder: 0,
    engineId: null,
    templateStepId: null,
    ...overrides,
  };
}

describe("ChecklistSection", () => {
  it("toggles an open item to done via the checkbox", async () => {
    const onSetStatus = vi.fn();
    const user = userEvent.setup();

    render(
      <ChecklistSection
        items={[item()]}
        engines={[]}
        canEdit
        onSetStatus={onSetStatus}
        onMove={vi.fn()}
        onReorder={vi.fn()}
        onAdd={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("checkbox", { name: 'Mark "Change oil" done' }));
    expect(onSetStatus).toHaveBeenCalledWith("item-1", "Done");
  });

  it("marks an item skipped via the context menu", async () => {
    const onSetStatus = vi.fn();
    const user = userEvent.setup();

    render(
      <ChecklistSection
        items={[item()]}
        engines={[]}
        canEdit
        onSetStatus={onSetStatus}
        onMove={vi.fn()}
        onReorder={vi.fn()}
        onAdd={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("button", { name: "Checklist item actions" }));
    await user.click(await screen.findByText("Mark skipped"));
    expect(onSetStatus).toHaveBeenCalledWith("item-1", "Skipped");
  });

  it("shows Reopen instead of Mark skipped once an item is already skipped", async () => {
    const user = userEvent.setup();
    render(
      <ChecklistSection
        items={[item({ status: "Skipped" })]}
        engines={[]}
        canEdit
        onSetStatus={vi.fn()}
        onMove={vi.fn()}
        onReorder={vi.fn()}
        onAdd={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("button", { name: "Checklist item actions" }));
    expect(await screen.findByText("Reopen")).toBeInTheDocument();
    expect(screen.queryByText("Mark skipped")).not.toBeInTheDocument();
  });

  it("moves an item down via the context menu fallback", async () => {
    const onMove = vi.fn();
    const user = userEvent.setup();

    render(
      <ChecklistSection
        items={[item({ id: "a", text: "First" }), item({ id: "b", text: "Second", sortOrder: 1 })]}
        engines={[]}
        canEdit
        onSetStatus={vi.fn()}
        onMove={onMove}
        onReorder={vi.fn()}
        onAdd={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    const menus = screen.getAllByRole("button", { name: "Checklist item actions" });
    await user.click(menus[0]);
    await user.click(await screen.findByText("Move down"));
    expect(onMove).toHaveBeenCalledWith("a", "down");
  });

  it("adds an ad hoc checklist item", async () => {
    const onAdd = vi.fn();
    const user = userEvent.setup();

    render(
      <ChecklistSection
        items={[]}
        engines={[]}
        canEdit
        onSetStatus={vi.fn()}
        onMove={vi.fn()}
        onReorder={vi.fn()}
        onAdd={onAdd}
        onDelete={vi.fn()}
      />
    );

    await user.type(screen.getByPlaceholderText("Add a checklist item…"), "Check trailer lights");
    await user.click(screen.getByRole("button", { name: "Add" }));
    expect(onAdd).toHaveBeenCalledWith("Check trailer lights");
  });

  it("hides edit controls when canEdit is false", () => {
    render(
      <ChecklistSection
        items={[item()]}
        engines={[]}
        canEdit={false}
        onSetStatus={vi.fn()}
        onMove={vi.fn()}
        onReorder={vi.fn()}
        onAdd={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    expect(screen.queryByPlaceholderText("Add a checklist item…")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Checklist item actions" })).not.toBeInTheDocument();
  });
});
