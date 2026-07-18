import { ArrowDown, ArrowUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { TemplateStepFormDialog } from "@/components/templates/TemplateStepFormDialog";
import type { CreateTemplateStepRequest, TemplateStepResponse } from "@/api/types";

interface TemplateStepListProps {
  steps: TemplateStepResponse[];
  canEdit: boolean;
  onAdd: (request: CreateTemplateStepRequest) => void;
  onEdit: (stepId: string, request: CreateTemplateStepRequest) => void;
  onDelete: (stepId: string) => void;
  onReorder: (stepIds: string[]) => void;
}

export function TemplateStepList({ steps, canEdit, onAdd, onEdit, onDelete, onReorder }: TemplateStepListProps) {
  const ordered = [...steps].sort((a, b) => a.sortOrder - b.sortOrder);

  function move(stepId: string, direction: "up" | "down") {
    const ids = ordered.map((s) => s.id);
    const index = ids.indexOf(stepId);
    const swapWith = direction === "up" ? index - 1 : index + 1;
    if (swapWith < 0 || swapWith >= ids.length) return;
    const next = [...ids];
    [next[index], next[swapWith]] = [next[swapWith], next[index]];
    onReorder(next);
  }

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-semibold">Steps</h4>
        {canEdit && <TemplateStepFormDialog onSave={onAdd} trigger={<Button size="sm" variant="outline">Add step</Button>} />}
      </div>

      {ordered.length === 0 ? (
        <p className="text-sm text-muted-foreground">No steps yet.</p>
      ) : (
        <ul className="space-y-1">
          {ordered.map((step, index) => (
            <li
              key={step.id}
              className="flex items-center gap-2 rounded-md border border-border bg-card px-2 py-1.5 text-sm"
            >
              <span className="flex-1">
                {step.text}
                {step.engineScoped && <span className="ml-2 text-xs text-muted-foreground">(per engine)</span>}
                {step.suggestedParts.length > 0 && (
                  <span className="ml-2 text-xs text-muted-foreground">
                    {step.suggestedParts.length} suggested part{step.suggestedParts.length === 1 ? "" : "s"}
                  </span>
                )}
              </span>
              {canEdit && (
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Move up"
                    disabled={index === 0}
                    onClick={() => move(step.id, "up")}
                  >
                    <ArrowUp className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label="Move down"
                    disabled={index === ordered.length - 1}
                    onClick={() => move(step.id, "down")}
                  >
                    <ArrowDown className="h-3.5 w-3.5" />
                  </Button>
                  <TemplateStepFormDialog
                    initial={step}
                    onSave={(request) => onEdit(step.id, request)}
                    trigger={
                      <Button size="sm" variant="outline">
                        Edit
                      </Button>
                    }
                  />
                  <Button size="sm" variant="outline" onClick={() => onDelete(step.id)}>
                    Delete
                  </Button>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
