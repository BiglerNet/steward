import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { PartsSection } from "@/components/maintenance/PartsSection";
import type { PartLineResponse } from "@/api/types";

function part(overrides: Partial<PartLineResponse> = {}): PartLineResponse {
  return {
    id: "part-1",
    maintenanceItemId: "mi-1",
    name: "Oil filter",
    partNumber: null,
    vendor: null,
    trackingNumber: null,
    orderUrl: null,
    quantity: 1,
    status: "Needed",
    cost: null,
    checklistItemId: null,
    partId: null,
    ...overrides,
  };
}

describe("PartsSection", () => {
  it("adds a part line with a name and quantity", async () => {
    const onAdd = vi.fn();
    const user = userEvent.setup();

    render(
      <PartsSection
        partLines={[]}
        canEdit
        onAdd={onAdd}
        onEdit={vi.fn()}
        onSetStatus={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("button", { name: "Add part" }));
    await user.type(screen.getByLabelText("Name"), "Spark plug");
    await user.clear(screen.getByLabelText("Quantity"));
    await user.type(screen.getByLabelText("Quantity"), "4");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(onAdd).toHaveBeenCalledWith(expect.objectContaining({ name: "Spark plug", quantity: 4 }));
  });

  it("advances a part's status from Needed to Ordered", async () => {
    const onSetStatus = vi.fn();
    const user = userEvent.setup();

    render(
      <PartsSection
        partLines={[part()]}
        canEdit
        onAdd={vi.fn()}
        onEdit={vi.fn()}
        onSetStatus={onSetStatus}
        onDelete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("combobox"));
    await user.click(await screen.findByRole("option", { name: "Ordered" }));

    expect(onSetStatus).toHaveBeenCalledWith("part-1", "Ordered");
  });

  it("deletes a part line", async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();

    render(
      <PartsSection
        partLines={[part()]}
        canEdit
        onAdd={vi.fn()}
        onEdit={vi.fn()}
        onSetStatus={vi.fn()}
        onDelete={onDelete}
      />
    );

    await user.click(screen.getByRole("button", { name: "Delete" }));
    expect(onDelete).toHaveBeenCalledWith("part-1");
  });

  it("hides add/edit/delete controls when canEdit is false", () => {
    render(
      <PartsSection
        partLines={[part()]}
        canEdit={false}
        onAdd={vi.fn()}
        onEdit={vi.fn()}
        onSetStatus={vi.fn()}
        onDelete={vi.fn()}
      />
    );

    expect(screen.queryByRole("button", { name: "Add part" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
    expect(screen.getByText("Needed")).toBeInTheDocument();
  });
});
