import { useDroppable } from "@dnd-kit/core";
import { KanbanCard } from "@/components/maintenance/KanbanCard";
import { cn } from "@/lib/utils";
import type { HouseholdMaintenanceItemResponse } from "@/api/types";

interface KanbanDoneDropZoneProps {
  items: HouseholdMaintenanceItemResponse[];
}

export function KanbanDoneDropZone({ items }: KanbanDoneDropZoneProps) {
  const { setNodeRef, isOver } = useDroppable({ id: "Done" });

  return (
    <div
      ref={setNodeRef}
      className={cn(
        "flex min-h-[200px] flex-col gap-2 rounded-lg border-2 border-dashed border-border bg-background p-3",
        isOver && "border-primary bg-primary/5"
      )}
    >
      <div className="flex items-center justify-between px-1">
        <h3 className="text-h3">Done</h3>
        <span className="text-sm text-muted-foreground">{items.length}</span>
      </div>
      {items.length === 0 ? (
        <p className="px-1 text-sm text-muted-foreground">
          Drag a card here to complete it. Completed items stay here for 7 days.
        </p>
      ) : (
        <>
          {items.map((item) => (
            <KanbanCard key={item.id} item={item} canEdit={false} onCancel={() => {}} />
          ))}
          <p className="px-1 text-xs text-muted-foreground">Completed items stay here for 7 days.</p>
        </>
      )}
    </div>
  );
}
