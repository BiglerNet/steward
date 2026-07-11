import type { RegistrationKind } from "@/api/types";
import { cn } from "@/lib/utils";

const LABELS: Record<RegistrationKind, string> = {
  Registration: "Registration",
  TrailPass: "Trail pass",
  Permit: "Permit",
};

const STYLES: Record<RegistrationKind, string> = {
  Registration: "bg-muted text-muted-foreground",
  TrailPass: "bg-accent text-accent-foreground",
  Permit: "bg-secondary text-secondary-foreground",
};

export interface RegistrationKindBadgeProps {
  kind: RegistrationKind;
}

export function RegistrationKindBadge({ kind }: RegistrationKindBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
        STYLES[kind]
      )}
    >
      {LABELS[kind]}
    </span>
  );
}
