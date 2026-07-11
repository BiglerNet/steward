import { cn } from "@/lib/utils";

interface WizardStepperProps {
  labels: string[];
  currentIndex: number;
}

export function WizardStepper({ labels, currentIndex }: WizardStepperProps) {
  return (
    <ol className="flex flex-wrap items-center gap-2">
      {labels.map((label, index) => (
        <li key={label} className="flex items-center gap-2">
          <span
            className={cn(
              "flex items-center gap-1.5 rounded-full border px-3 py-1 text-small font-medium",
              index === currentIndex
                ? "border-primary bg-primary text-primary-foreground"
                : index < currentIndex
                  ? "border-primary text-primary"
                  : "border-border text-muted-foreground"
            )}
          >
            <span>{index + 1}</span>
            <span>{label}</span>
          </span>
          {index < labels.length - 1 && <span className="text-muted-foreground">→</span>}
        </li>
      ))}
    </ol>
  );
}
