import type { Powertrain } from "@/api/types";
import { cn } from "@/lib/utils";

const STYLES: Record<Powertrain, string> = {
  Electric: "bg-accent text-accent-foreground",
  Hybrid: "bg-secondary text-secondary-foreground",
  "Plug-in Hybrid": "bg-secondary text-secondary-foreground",
};

export interface PowertrainBadgeProps {
  powertrain: Powertrain;
}

export function PowertrainBadge({ powertrain }: PowertrainBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        STYLES[powertrain]
      )}
    >
      {powertrain}
    </span>
  );
}
