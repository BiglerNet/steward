import { useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router";
import { getAsset } from "@/api/assets";
import { QuickCreateMaintenanceItemDialog } from "@/components/maintenance/QuickCreateMaintenanceItemDialog";
import { BlockedBadge, MaintenanceStatusBadge } from "@/components/maintenance/MaintenanceStatusBadge";
import { MaintenanceScheduleSection } from "@/components/maintenance/MaintenanceScheduleSection";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useMaintenanceItems } from "@/hooks/useMaintenanceItems";
import { useHouseholdRole } from "@/lib/permissions";

export function MaintenanceItemsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const navigate = useNavigate();
  const { canEdit } = useHouseholdRole();
  const { data: items } = useMaintenanceItems(householdId, assetId);
  const { data: asset } = useQuery({
    queryKey: ["households", householdId, "assets", assetId],
    queryFn: () => getAsset(householdId, assetId),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">Maintenance</h2>
        {canEdit && asset && (
          <QuickCreateMaintenanceItemDialog
            householdId={householdId}
            assetId={assetId}
            assetCategory={asset.category}
          />
        )}
      </div>

      <MaintenanceScheduleSection householdId={householdId} assetId={assetId} />

      {!items || items.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No maintenance items yet. Log the first entry.
        </p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Date</TableHead>
              <TableHead>Cost</TableHead>
              <TableHead>Completed</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((item) => (
              <TableRow
                key={item.id}
                className="cursor-pointer"
                onClick={() =>
                  navigate(`/households/${householdId}/assets/${assetId}/maintenance/${item.id}`, {
                    state: {
                      from: `/households/${householdId}/assets/${assetId}/maintenance`,
                      fromLabel: asset?.name,
                    },
                  })
                }
              >
                <TableCell>{item.title}</TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <MaintenanceStatusBadge status={item.status} />
                    {item.isBlocked && <BlockedBadge />}
                  </div>
                </TableCell>
                <TableCell>{item.date ?? "—"}</TableCell>
                <TableCell>{item.cost ?? "—"}</TableCell>
                <TableCell>
                  {item.completedAt ? new Date(item.completedAt).toLocaleDateString() : "—"}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
