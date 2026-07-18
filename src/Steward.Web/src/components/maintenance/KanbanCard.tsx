import { useDraggable } from "@dnd-kit/core";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, MoreVertical } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router";
import { BlockedBadge } from "@/components/maintenance/MaintenanceStatusBadge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { formatRelativeToNow } from "@/lib/relativeTime";
import { cn } from "@/lib/utils";
import type { HouseholdMaintenanceItemResponse } from "@/api/types";

interface KanbanCardProps {
  item: HouseholdMaintenanceItemResponse;
  canEdit: boolean;
  onCancel: (item: HouseholdMaintenanceItemResponse) => void;
}

export function KanbanCard({ item, canEdit, onCancel }: KanbanCardProps) {
  const { householdId } = useParams() as { householdId: string };
  const location = useLocation();
  const navigate = useNavigate();
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: item.id,
    disabled: !canEdit,
  });

  const style = {
    transform: CSS.Translate.toString(transform),
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      data-item-id={item.id}
      {...(canEdit ? attributes : {})}
      {...(canEdit ? listeners : {})}
      className={cn(
        "flex items-start gap-2 rounded-md border border-border bg-card p-3 shadow-sm",
        canEdit && "cursor-grab active:cursor-grabbing",
        isDragging && "z-10 opacity-70"
      )}
    >
      {canEdit && (
        <span aria-hidden="true" className="mt-0.5 text-muted-foreground">
          <GripVertical className="h-4 w-4" />
        </span>
      )}
      <div className="min-w-0 flex-1">
        <button
          type="button"
          onClick={() =>
            navigate(`/households/${householdId}/assets/${item.assetId}/maintenance/${item.id}`, {
              state: { from: `${location.pathname}${location.search}`, fromLabel: "Maintenance" },
            })
          }
          className="block w-full truncate text-left font-medium hover:underline"
        >
          {item.title}
        </button>
        <p className="truncate text-sm text-muted-foreground">{item.assetName}</p>
        {item.completedAt && (
          <p className="truncate text-xs text-muted-foreground">
            Completed {formatRelativeToNow(new Date(item.completedAt))}
          </p>
        )}
        {item.isBlocked && (
          <div className="mt-1">
            <BlockedBadge />
          </div>
        )}
      </div>
      {canEdit && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" aria-label={`"${item.title}" actions`}>
              <MoreVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onSelect={() => onCancel(item)} className="text-destructive">
              Cancel
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </div>
  );
}
