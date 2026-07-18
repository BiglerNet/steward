import { useState } from "react";
import {
  DndContext,
  KeyboardSensor,
  PointerSensor,
  TouchSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { useParams, useSearchParams } from "react-router";
import { toast } from "sonner";
import { DoneTransitionDialog } from "@/components/maintenance/DoneTransitionDialog";
import { KanbanColumn } from "@/components/maintenance/KanbanColumn";
import { KanbanDoneDropZone } from "@/components/maintenance/KanbanDoneDropZone";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useAssets } from "@/hooks/useAssets";
import { useHouseholdMaintenanceItems } from "@/hooks/useHouseholdMaintenanceItems";
import { useKanbanMutations } from "@/hooks/useKanbanMutations";
import { isRecentlyCompleted, planDrop } from "@/lib/kanban";
import { useHouseholdRole } from "@/lib/permissions";
import type { HouseholdMaintenanceItemResponse, MaintenanceItemStatus } from "@/api/types";

const ALL_ASSETS = "all";

const COLUMNS: { id: MaintenanceItemStatus; label: string }[] = [
  { id: "Planned", label: "Planned" },
  { id: "InProgress", label: "In Progress" },
];

export function MaintenanceKanbanPage() {
  const { householdId } = useParams() as { householdId: string };
  const { canEdit } = useHouseholdRole();
  const [searchParams, setSearchParams] = useSearchParams();
  const assetFilter = searchParams.get("asset") ?? ALL_ASSETS;
  const [pendingDoneItem, setPendingDoneItem] = useState<HouseholdMaintenanceItemResponse | null>(null);
  const [transitionPending, setTransitionPending] = useState(false);

  const { data: assets } = useAssets(householdId);
  const { data: items } = useHouseholdMaintenanceItems(householdId, {
    status: ["Planned", "InProgress", "Done"],
    assetId: assetFilter === ALL_ASSETS ? undefined : assetFilter,
  });
  const doneRecently = (items ?? []).filter((item) => isRecentlyCompleted(item));
  const { patchStatus, skipChecklistItem } = useKanbanMutations(householdId);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 5 } }),
    useSensor(KeyboardSensor)
  );

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over) return;

    const item = items?.find((i) => i.id === String(active.id));
    if (!item) return;

    const plan = planDrop(item, String(over.id) as MaintenanceItemStatus);
    if (plan.type === "noop") return;
    if (plan.type === "confirmDone") {
      setPendingDoneItem(item);
      return;
    }

    patchStatus.mutate(
      { assetId: item.assetId, itemId: item.id, status: plan.status },
      { onError: () => toast.error("Couldn't update this item.") }
    );
  }

  function handleAssetFilterChange(value: string) {
    setSearchParams(value === ALL_ASSETS ? {} : { asset: value });
  }

  function handleCancel(item: HouseholdMaintenanceItemResponse) {
    patchStatus.mutate(
      { assetId: item.assetId, itemId: item.id, status: "Cancelled" },
      { onError: () => toast.error("Couldn't cancel this item.") }
    );
  }

  async function handleCompleteAnyway() {
    if (!pendingDoneItem) return;
    setTransitionPending(true);
    try {
      await patchStatus.mutateAsync({
        assetId: pendingDoneItem.assetId,
        itemId: pendingDoneItem.id,
        status: "Done",
      });
      setPendingDoneItem(null);
    } catch {
      toast.error("Couldn't complete this item.");
    } finally {
      setTransitionPending(false);
    }
  }

  async function handleSkipRemainingThenComplete() {
    if (!pendingDoneItem) return;
    setTransitionPending(true);
    try {
      const openItems = pendingDoneItem.checklistItems.filter((c) => c.status === "Open");
      await Promise.all(
        openItems.map((c) =>
          skipChecklistItem.mutateAsync({
            assetId: pendingDoneItem.assetId,
            itemId: pendingDoneItem.id,
            checklistItemId: c.id,
          })
        )
      );
      await patchStatus.mutateAsync({
        assetId: pendingDoneItem.assetId,
        itemId: pendingDoneItem.id,
        status: "Done",
      });
      setPendingDoneItem(null);
    } catch {
      toast.error("Couldn't complete this item.");
    } finally {
      setTransitionPending(false);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        <h2 className="text-h2">Maintenance</h2>
        <Select value={assetFilter} onValueChange={handleAssetFilterChange}>
          <SelectTrigger className="w-[220px]" aria-label="Filter by asset">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_ASSETS}>All assets</SelectItem>
            {assets?.map((asset) => (
              <SelectItem key={asset.id} value={asset.id}>
                {asset.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {COLUMNS.map((column) => (
            <KanbanColumn
              key={column.id}
              id={column.id}
              label={column.label}
              items={(items ?? []).filter((item) => item.status === column.id)}
              canEdit={canEdit}
              onCancel={handleCancel}
            />
          ))}
          <KanbanDoneDropZone items={doneRecently} />
        </div>

        <DoneTransitionDialog
          open={pendingDoneItem !== null}
          openItemCount={pendingDoneItem?.checklistItems.filter((c) => c.status === "Open").length ?? 0}
          pending={transitionPending}
          onGoBack={() => setPendingDoneItem(null)}
          onCompleteAnyway={handleCompleteAnyway}
          onSkipRemainingThenComplete={handleSkipRemainingThenComplete}
        />
      </DndContext>
    </div>
  );
}
