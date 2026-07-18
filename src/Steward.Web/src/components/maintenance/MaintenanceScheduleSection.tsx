import { DueStatusBadge } from "@/components/maintenance/DueStatusBadge";
import { useMaintenanceSchedule } from "@/hooks/useMaintenanceSchedule";
import type { MaintenanceScheduleEntryResponse } from "@/api/types";

function formatLastDone(entry: MaintenanceScheduleEntryResponse): string {
  if (!entry.lastDoneAt) return "Never";

  const date = new Date(entry.lastDoneAt).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });

  if (!entry.lastDoneReading) return date;

  const unit = entry.lastDoneReading.unit === "Hours" ? "hrs" : "mi";
  return `${date} · ${entry.lastDoneReading.value.toLocaleString()} ${unit}`;
}

interface MaintenanceScheduleSectionProps {
  householdId: string;
  assetId: string;
}

export function MaintenanceScheduleSection({ householdId, assetId }: MaintenanceScheduleSectionProps) {
  const { data: entries } = useMaintenanceSchedule(householdId, assetId);

  if (!entries || entries.length === 0) {
    return null;
  }

  return (
    <div className="overflow-hidden rounded-lg border border-border bg-card">
      <div className="border-b border-border bg-background px-5 py-3.5 text-h3">
        Maintenance schedule
      </div>
      <ul className="divide-y divide-border">
        {entries.map((entry) => (
          <li
            key={`${entry.templateStepId}:${entry.engineId ?? ""}`}
            className="flex items-center justify-between gap-3 px-5 py-3"
          >
            <div>
              <p className="font-medium">
                {entry.stepText}
                {entry.engineLabel ? ` · ${entry.engineLabel}` : ""}
              </p>
              <p className="text-sm text-muted-foreground">Last done: {formatLastDone(entry)}</p>
            </div>
            <DueStatusBadge status={entry.dueStatus} />
          </li>
        ))}
      </ul>
    </div>
  );
}
