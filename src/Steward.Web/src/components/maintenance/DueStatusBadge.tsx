import { cn } from "@/lib/utils";
import type { MaintenanceDueStatus } from "@/api/types";

const STATUS_LABELS: Record<MaintenanceDueStatus, string> = {
  Overdue: "Overdue",
  DueSoon: "Due soon",
  Upcoming: "Upcoming",
  OK: "OK",
  Unknown: "Unknown",
};

const STATUS_CLASSES: Record<MaintenanceDueStatus, string> = {
  Overdue: "bg-danger-bg text-danger",
  DueSoon: "bg-warning-bg text-warning",
  Upcoming: "bg-primary/15 text-primary",
  OK: "bg-success-bg text-success",
  Unknown: "bg-muted text-muted-foreground",
};

export function DueStatusBadge({ status }: { status: MaintenanceDueStatus }) {
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
