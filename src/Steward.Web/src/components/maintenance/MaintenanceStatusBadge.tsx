import { cn } from "@/lib/utils";
import type { MaintenanceItemStatus } from "@/api/types";

const STATUS_LABELS: Record<MaintenanceItemStatus, string> = {
  Planned: "Planned",
  InProgress: "In Progress",
  Done: "Done",
  Cancelled: "Cancelled",
};

const STATUS_CLASSES: Record<MaintenanceItemStatus, string> = {
  Planned: "bg-muted text-muted-foreground",
  InProgress: "bg-primary/15 text-primary",
  Done: "bg-emerald-500/15 text-emerald-600 dark:text-emerald-400",
  Cancelled: "bg-muted text-muted-foreground line-through",
};

export function MaintenanceStatusBadge({ status }: { status: MaintenanceItemStatus }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        STATUS_CLASSES[status]
      )}
    >
      {STATUS_LABELS[status]}
    </span>
  );
}

export function BlockedBadge() {
  return (
    <span className="inline-flex items-center rounded-full bg-destructive/15 px-2 py-0.5 text-xs font-medium text-destructive">
      Blocked
    </span>
  );
}
