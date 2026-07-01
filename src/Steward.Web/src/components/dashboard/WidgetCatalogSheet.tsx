import { useState } from "react";
import { Settings2, ArrowUp, ArrowDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { useDashboard } from "@/hooks/useDashboards";
import { useReplaceWidgetLayout } from "@/hooks/useDashboardMutations";
import type { WidgetDefinition, WidgetSize, WidgetType } from "@/api/types";
import { toast } from "sonner";

const CATALOG: { type: WidgetType; label: string; defaultSize: WidgetSize }[] = [
  { type: "AssetCount", label: "Asset Count", defaultSize: "Small" },
  { type: "CylinderIndex", label: "Cylinder Index", defaultSize: "Small" },
  { type: "TotalDisplacement", label: "Total Displacement", defaultSize: "Small" },
  { type: "TotalHorsepower", label: "Total Horsepower", defaultSize: "Small" },
  { type: "TotalTorque", label: "Total Torque", defaultSize: "Small" },
  { type: "DueSoon", label: "Due Soon", defaultSize: "Full" },
  { type: "RecentActivity", label: "Recent Activity", defaultSize: "Full" },
  { type: "FuelCostYtd", label: "Fuel Cost YTD", defaultSize: "Wide" },
  { type: "MileageMtd", label: "Mileage MTD", defaultSize: "Wide" },
];

const SIZE_OPTIONS: WidgetSize[] = ["Small", "Wide", "Full"];

interface WidgetCatalogSheetProps {
  householdId: string;
  dashboardId: string;
}

interface LayoutItem {
  widgetType: WidgetType;
  widgetSize: WidgetSize;
  config: string | null;
}

export function WidgetCatalogSheet({ householdId, dashboardId }: WidgetCatalogSheetProps) {
  const [open, setOpen] = useState(false);
  const { data: detail } = useDashboard(householdId, dashboardId);
  const replace = useReplaceWidgetLayout(householdId, dashboardId);

  const initialLayout = (): LayoutItem[] =>
    (detail?.widgets ?? []).map((w) => ({
      widgetType: w.widgetType,
      widgetSize: w.widgetSize,
      config: w.config ?? null,
    }));

  const [layout, setLayout] = useState<LayoutItem[]>(initialLayout);

  function handleOpen(isOpen: boolean) {
    if (isOpen) setLayout(initialLayout());
    setOpen(isOpen);
  }

  function isInLayout(type: WidgetType) {
    return layout.some((w) => w.widgetType === type);
  }

  function toggleWidget(type: WidgetType, defaultSize: WidgetSize) {
    if (isInLayout(type)) {
      setLayout((prev) => prev.filter((w) => w.widgetType !== type));
    } else {
      setLayout((prev) => [...prev, { widgetType: type, widgetSize: defaultSize, config: null }]);
    }
  }

  function changeSize(type: WidgetType, size: WidgetSize) {
    setLayout((prev) =>
      prev.map((w) => (w.widgetType === type ? { ...w, widgetSize: size } : w))
    );
  }

  function moveUp(index: number) {
    if (index === 0) return;
    setLayout((prev) => {
      const next = [...prev];
      [next[index - 1], next[index]] = [next[index], next[index - 1]];
      return next;
    });
  }

  function moveDown(index: number) {
    if (index >= layout.length - 1) return;
    setLayout((prev) => {
      const next = [...prev];
      [next[index], next[index + 1]] = [next[index + 1], next[index]];
      return next;
    });
  }

  async function handleSave() {
    const widgets: WidgetDefinition[] = layout.map((w) => ({
      widgetType: w.widgetType,
      widgetSize: w.widgetSize,
      config: w.config ?? undefined,
    }));
    try {
      await replace.mutateAsync({ widgets });
      setOpen(false);
      toast.success("Dashboard layout saved.");
    } catch {
      toast.error("Failed to save layout.");
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Settings2 className="mr-2 h-4 w-4" />
          Edit Dashboard
        </Button>
      </DialogTrigger>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit Dashboard Layout</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div>
            <p className="text-sm font-medium mb-2">Available Widgets</p>
            <div className="flex flex-wrap gap-2">
              {CATALOG.map(({ type, label, defaultSize }) => (
                <button
                  key={type}
                  onClick={() => toggleWidget(type, defaultSize)}
                  className={`rounded-full border px-3 py-1 text-xs transition-colors ${
                    isInLayout(type)
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-background text-muted-foreground hover:text-foreground"
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {layout.length > 0 && (
            <div>
              <p className="text-sm font-medium mb-2">Layout Order</p>
              <ul className="space-y-1">
                {layout.map((item, i) => {
                  const entry = CATALOG.find((c) => c.type === item.widgetType);
                  return (
                    <li
                      key={item.widgetType}
                      className="flex items-center gap-2 rounded-md border border-border bg-card px-3 py-2"
                    >
                      <span className="flex-1 text-sm">{entry?.label ?? item.widgetType}</span>
                      <select
                        value={item.widgetSize}
                        onChange={(e) => changeSize(item.widgetType, e.target.value as WidgetSize)}
                        className="rounded border border-border bg-background px-1 py-0.5 text-xs"
                      >
                        {SIZE_OPTIONS.map((s) => (
                          <option key={s} value={s}>
                            {s}
                          </option>
                        ))}
                      </select>
                      <button
                        onClick={() => moveUp(i)}
                        disabled={i === 0}
                        className="text-muted-foreground hover:text-foreground disabled:opacity-30"
                        aria-label="Move up"
                      >
                        <ArrowUp className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => moveDown(i)}
                        disabled={i === layout.length - 1}
                        className="text-muted-foreground hover:text-foreground disabled:opacity-30"
                        aria-label="Move down"
                      >
                        <ArrowDown className="h-4 w-4" />
                      </button>
                    </li>
                  );
                })}
              </ul>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={replace.isPending}>
            {replace.isPending ? "Saving…" : "Save Layout"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
