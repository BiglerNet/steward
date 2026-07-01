import { useState } from "react";
import { useParams } from "react-router";
import { DashboardManagerBar } from "@/components/dashboard/DashboardManagerBar";
import { WidgetCatalogSheet } from "@/components/dashboard/WidgetCatalogSheet";
import { WidgetGrid } from "@/components/dashboard/WidgetGrid";
import { useDashboard, useDashboards } from "@/hooks/useDashboards";
import { useDashboardSnapshot } from "@/hooks/useDashboardSnapshot";
import { readLastDashboardId, writeLastDashboardId } from "@/lib/dashboardStorage";
import { useHouseholdRole } from "@/lib/permissions";
import type { DashboardSummaryResponse } from "@/api/types";

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

  const resolvedId =
    activeDashboardId ?? (dashboards ? pickInitialDashboard(householdId, dashboards) : null);

  const { data: detail, isLoading: detailLoading } = useDashboard(householdId, resolvedId);
  const { data: snapshot, isLoading: snapshotLoading } = useDashboardSnapshot(
    householdId,
    resolvedId
  );

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
  }

  function handleDeleted(deletedId: string) {
    // Pick any remaining dashboard that isn't the one just deleted
    const remaining = dashboards!.filter((d) => d.id !== deletedId);
    const next = remaining.find((d) => d.isDefault) ?? remaining[0];
    if (next) handleSelect(next.id);
  }

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
          <WidgetCatalogSheet householdId={householdId} dashboardId={currentId} />
        )}
      </div>

      {detailLoading || snapshotLoading ? (
        <div className="py-8 text-center text-muted-foreground">Loading…</div>
      ) : (
        <WidgetGrid widgets={detail?.widgets ?? []} snapshot={snapshot ?? {}} />
      )}
    </div>
  );
}
