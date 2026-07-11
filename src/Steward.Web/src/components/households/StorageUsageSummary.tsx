import { formatBytes } from "@/lib/formatBytes";
import { cn } from "@/lib/utils";

export interface StorageUsageSummaryProps {
  usedBytes: number;
  quotaBytes: number;
}

const WARNING_THRESHOLD = 0.9;

export function StorageUsageSummary({ usedBytes, quotaBytes }: StorageUsageSummaryProps) {
  const ratio = quotaBytes > 0 ? usedBytes / quotaBytes : 0;
  const percent = Math.min(100, Math.round(ratio * 100));
  const isNearFull = ratio >= WARNING_THRESHOLD;

  return (
    <div className="space-y-2">
      <p className="text-body">
        {formatBytes(usedBytes)} of {formatBytes(quotaBytes)}
      </p>
      <div
        role="progressbar"
        aria-valuenow={percent}
        aria-valuemin={0}
        aria-valuemax={100}
        className="h-2 w-full overflow-hidden rounded-full bg-muted"
      >
        <div
          className={cn("h-full rounded-full transition-[width]", isNearFull ? "bg-warning" : "bg-primary")}
          style={{ width: `${percent}%` }}
        />
      </div>
      {isNearFull && <p className="text-small text-warning">Storage is almost full.</p>}
    </div>
  );
}
