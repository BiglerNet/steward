import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { WidgetResponse } from "@/api/types";
import { reorderWidgets, WidgetGrid } from "@/components/dashboard/WidgetGrid";

const widgets: WidgetResponse[] = [
  { id: "w1", widgetType: "CylinderIndex", widgetSize: "Small", position: 0, config: null },
  { id: "w2", widgetType: "TotalDisplacement", widgetSize: "Small", position: 1, config: null },
  { id: "w3", widgetType: "AssetCount", widgetSize: "Small", position: 2, config: null },
];

describe("reorderWidgets", () => {
  it("moves a widget forward and renumbers positions", () => {
    const result = reorderWidgets(widgets, "w1", "w3");
    expect(result?.map((w) => w.id)).toEqual(["w2", "w3", "w1"]);
    expect(result?.map((w) => w.position)).toEqual([0, 1, 2]);
  });

  it("moves a widget backward", () => {
    const result = reorderWidgets(widgets, "w3", "w1");
    expect(result?.map((w) => w.id)).toEqual(["w3", "w1", "w2"]);
  });

  it("returns null for an unknown id", () => {
    expect(reorderWidgets(widgets, "missing", "w1")).toBeNull();
  });
});

describe("WidgetGrid", () => {
  it("hides drag/resize/remove controls when not editing", () => {
    render(<WidgetGrid widgets={widgets} snapshot={{}} />);
    expect(screen.queryByLabelText("Drag to reorder widget")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Resize widget")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Remove widget")).not.toBeInTheDocument();
  });

  it("shows drag/resize/remove controls per widget when editing", () => {
    render(
      <WidgetGrid widgets={widgets} snapshot={{}} isEditing onResize={vi.fn()} onRemove={vi.fn()} />
    );
    expect(screen.getAllByLabelText("Drag to reorder widget")).toHaveLength(3);
    expect(screen.getAllByLabelText("Resize widget")).toHaveLength(3);
    expect(screen.getAllByLabelText("Remove widget")).toHaveLength(3);
  });

  it("cycles a widget's resize tooltip through Small -> Wide -> Full -> Small", () => {
    const sizes: Array<WidgetResponse["widgetSize"]> = ["Small", "Wide", "Full"];
    for (const size of sizes) {
      const { unmount } = render(
        <WidgetGrid
          widgets={[{ ...widgets[0], widgetSize: size }]}
          snapshot={{}}
          isEditing
          onResize={vi.fn()}
          onRemove={vi.fn()}
        />
      );
      const expectedNext = size === "Small" ? "Wide" : size === "Wide" ? "Full" : "Small";
      expect(screen.getByLabelText("Resize widget")).toHaveAttribute(
        "title",
        `Resize (${size} → ${expectedNext})`
      );
      unmount();
    }
  });

  it("calls onResize/onRemove with the widget's id when its controls are clicked", async () => {
    const onResize = vi.fn();
    const onRemove = vi.fn();
    render(
      <WidgetGrid widgets={widgets} snapshot={{}} isEditing onResize={onResize} onRemove={onRemove} />
    );

    const w2 = document.querySelector('[data-widget-id="w2"]');
    if (!w2) throw new Error("widget w2 not found");

    const user = userEvent.setup();
    await user.click(within(w2 as HTMLElement).getByLabelText("Resize widget"));
    await user.click(within(w2 as HTMLElement).getByLabelText("Remove widget"));

    expect(onResize).toHaveBeenCalledWith("w2");
    expect(onRemove).toHaveBeenCalledWith("w2");
  });
});
