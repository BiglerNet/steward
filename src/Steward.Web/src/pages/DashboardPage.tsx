import { useState } from "react";
import { Plus, Settings2 } from "lucide-react";
import { useParams } from "react-router";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { DashboardManagerBar } from "@/components/dashboard/DashboardManagerBar";
import { WidgetGrid } from "@/components/dashboard/WidgetGrid";
import { useDashboard, useDashboards } from "@/hooks/useDashboards";
import { useReplaceWidgetLayout } from "@/hooks/useDashboardMutations";
import { useDashboardSnapshot } from "@/hooks/useDashboardSnapshot";
import { readLastDashboardId, writeLastDashboardId } from "@/lib/dashboardStorage";
import { useHouseholdRole } from "@/lib/permissions";
import { WIDGET_CATALOG } from "@/lib/widgetCatalog";
import type { DashboardSummaryResponse, WidgetResponse, WidgetSize } from "@/api/types";

const NEXT_SIZE: Record<WidgetSize, WidgetSize> = {
  Small: "Wide",
  Wide: "Full",
  Full: "Small",
};

function pickInitialDashboard(
  householdId: string,
  dashboards: DashboardSummaryResponse[]
): string | null {
  if (dashboards.length === 0) return null;
  const lastId = readLastDashboardId(householdId);
  if (lastId && dashboards.some((d) => d.id === lastId)) return lastId;
  return (dashboards.find((d) => d.isDefault) ?? dashboards[0]).id;
}

export function DashboardPage() {
  const { householdId } = useParams() as { householdId: string };
  const { canEdit } = useHouseholdRole();
  const { data: dashboards, isLoading: dashboardsLoading } = useDashboards(householdId);
  const [activeDashboardId, setActiveDashboardId] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [stagedWidgets, setStagedWidgets] = useState<WidgetResponse[]>([]);

  const resolvedId =
    activeDashboardId ?? (dashboards ? pickInitialDashboard(householdId, dashboards) : null);

  const { data: detail, isLoading: detailLoading } = useDashboard(householdId, resolvedId);
  const { data: snapshot, isLoading: snapshotLoading } = useDashboardSnapshot(
    householdId,
    resolvedId
  );
  const replace = useReplaceWidgetLayout(householdId, resolvedId ?? "");

  if (dashboardsLoading) {
    return <div className="py-12 text-center text-muted-foreground">Loading dashboards…</div>;
  }

  if (!dashboards || dashboards.length === 0) {
    return <div className="py-12 text-center text-muted-foreground">No dashboards found.</div>;
  }

  const currentId = resolvedId ?? dashboards[0].id;

  function handleSelect(id: string) {
    writeLastDashboardId(householdId, id);
    setActiveDashboardId(id);
    setIsEditing(false);
  }

  function handleDeleted(deletedId: string) {
    // Pick any remaining dashboard that isn't the one just deleted
    const remaining = dashboards!.filter((d) => d.id !== deletedId);
    const next = remaining.find((d) => d.isDefault) ?? remaining[0];
    if (next) handleSelect(next.id);
  }

  function handleEnterEdit() {
    setStagedWidgets(detail?.widgets ?? []);
    setIsEditing(true);
  }

  function handleCancel() {
    setIsEditing(false);
    setStagedWidgets([]);
  }

  async function handleSave() {
    try {
      await replace.mutateAsync({
        widgets: stagedWidgets.map((w) => ({
          widgetType: w.widgetType,
          widgetSize: w.widgetSize,
          config: w.config ?? undefined,
        })),
      });
      setIsEditing(false);
      setStagedWidgets([]);
      toast.success("Dashboard layout saved.");
    } catch {
      toast.error("Failed to save layout.");
    }
  }

  function handleResizeWidget(widgetId: string) {
    setStagedWidgets((prev) =>
      prev.map((w) => (w.id === widgetId ? { ...w, widgetSize: NEXT_SIZE[w.widgetSize] } : w))
    );
  }

  function handleRemoveWidget(widgetId: string) {
    setStagedWidgets((prev) => prev.filter((w) => w.id !== widgetId));
  }

  function handleAddWidget(type: (typeof WIDGET_CATALOG)[number]) {
    setStagedWidgets((prev) => [
      ...prev,
      {
        id: crypto.randomUUID(),
        widgetType: type.type,
        widgetSize: type.defaultSize,
        position: prev.length,
        config: null,
      },
    ]);
  }

  const availableToAdd = WIDGET_CATALOG.filter(
    (entry) => !stagedWidgets.some((w) => w.widgetType === entry.type)
  );

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4 flex-wrap">
        <DashboardManagerBar
          householdId={householdId}
          dashboards={dashboards}
          activeDashboardId={currentId}
          canEdit={canEdit}
          onSelect={handleSelect}
          onDeleted={handleDeleted}
        />
        {canEdit && (
          <div className="flex items-center gap-2">
            {isEditing ? (
              <>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm" disabled={availableToAdd.length === 0}>
                      <Plus className="mr-2 h-4 w-4" />
                      Add Widget
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {availableToAdd.map((entry) => (
                      <DropdownMenuItem key={entry.type} onSelect={() => handleAddWidget(entry)}>
                        {entry.label}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
                <Button variant="outline" size="sm" onClick={handleCancel} disabled={replace.isPending}>
                  Cancel
                </Button>
                <Button size="sm" onClick={handleSave} disabled={replace.isPending}>
                  {replace.isPending ? "Saving…" : "Save Layout"}
                </Button>
              </>
            ) : (
              <Button variant="outline" size="sm" onClick={handleEnterEdit}>
                <Settings2 className="mr-2 h-4 w-4" />
                Edit Dashboard
              </Button>
            )}
          </div>
        )}
      </div>

      {detailLoading || snapshotLoading ? (
        <div className="py-8 text-center text-muted-foreground">Loading…</div>
      ) : (
        <WidgetGrid
          widgets={isEditing ? stagedWidgets : (detail?.widgets ?? [])}
          snapshot={snapshot ?? {}}
          isEditing={isEditing}
          onReorder={setStagedWidgets}
          onResize={handleResizeWidget}
          onRemove={handleRemoveWidget}
        />
      )}
    </div>
  );
}
