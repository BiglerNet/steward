import { getExpiryStatus } from "@/lib/expiry";
import { cn } from "@/lib/utils";

const LABELS = {
  overdue: "Overdue",
  dueSoon: "Due soon",
  ok: "OK",
} as const;

const STYLES = {
  overdue: "bg-danger-bg text-danger",
  dueSoon: "bg-warning-bg text-warning",
  ok: "bg-muted text-muted-foreground",
} as const;

export interface ExpiryBadgeProps {
  expiresOn: string | null;
}

export function ExpiryBadge({ expiresOn }: ExpiryBadgeProps) {
  const status = getExpiryStatus(expiresOn);

  if (status === "none") {
    return null;
  }

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        STYLES[status]
      )}
    >
      {LABELS[status]}
    </span>
  );
}
