import { useDroppable } from "@dnd-kit/core";
import { KanbanCard } from "@/components/maintenance/KanbanCard";
import { cn } from "@/lib/utils";
import type { HouseholdMaintenanceItemResponse, MaintenanceItemStatus } from "@/api/types";

interface KanbanColumnProps {
  id: MaintenanceItemStatus;
  label: string;
  items: HouseholdMaintenanceItemResponse[];
  canEdit: boolean;
  onCancel: (item: HouseholdMaintenanceItemResponse) => void;
}

export function KanbanColumn({ id, label, items, canEdit, onCancel }: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id });

  return (
    <div
      ref={setNodeRef}
      className={cn(
        "flex min-h-[200px] flex-col gap-2 rounded-lg border border-border bg-background p-3",
        isOver && "border-primary bg-primary/5"
      )}
    >
      <div className="flex items-center justify-between px-1">
        <h3 className="text-h3">{label}</h3>
        <span className="text-sm text-muted-foreground">{items.length}</span>
      </div>
      {items.length === 0 ? (
        <p className="px-1 text-sm text-muted-foreground">No items.</p>
      ) : (
        items.map((item) => <KanbanCard key={item.id} item={item} canEdit={canEdit} onCancel={onCancel} />)
      )}
    </div>
  );
}
