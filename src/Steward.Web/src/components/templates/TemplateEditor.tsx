import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TemplateStepList } from "@/components/templates/TemplateStepList";
import { parseOptionalText } from "@/lib/formHelpers";
import type { useTemplateEditorMutations } from "@/hooks/useTemplateEditorMutations";
import type { AssetCategory, TemplateResponse } from "@/api/types";

const ALL_CATEGORIES: AssetCategory[] = [
  "Car",
  "Truck",
  "Suv",
  "Van",
  "Motorcycle",
  "Utv",
  "Atv",
  "Snowmobile",
  "DirtBike",
  "GolfCart",
  "PowerBoat",
  "Sailboat",
  "Pwc",
  "UtilityTrailer",
  "EnclosedTrailer",
  "SnowmobileTrailer",
  "BoatTrailer",
  "RidingMower",
  "PowerWasher",
  "Generator",
  "SmallEngine",
];

interface TemplateEditorProps {
  template: TemplateResponse;
  canEdit: boolean;
  mutations: ReturnType<typeof useTemplateEditorMutations>;
  onDeleted?: () => void;
}

export function TemplateEditor({ template, canEdit, mutations, onDeleted }: TemplateEditorProps) {
  const [title, setTitle] = useState(template.title);
  const [description, setDescription] = useState(template.description ?? "");

  function patch(fields: Parameters<typeof mutations.patchTemplate.mutate>[0]["request"]) {
    mutations.patchTemplate.mutate(
      { templateId: template.id, request: fields },
      { onError: () => toast.error("Couldn't save this template.") }
    );
  }

  function toggleCategory(category: AssetCategory) {
    const next = template.applicableCategories.includes(category)
      ? template.applicableCategories.filter((c) => c !== category)
      : [...template.applicableCategories, category];
    patch({ applicableCategories: next });
  }

  function handleDelete() {
    if (window.confirm(`Delete "${template.title}"? This can't be undone.`)) {
      mutations.deleteTemplate.mutate(template.id, {
        onSuccess: onDeleted,
        onError: () => toast.error("Couldn't delete this template."),
      });
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-4">
        {canEdit ? (
          <Input
            className="text-h3 h-auto border-none px-0 shadow-none focus-visible:ring-0"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => title.trim() && title !== template.title && patch({ title: title.trim() })}
          />
        ) : (
          <h3 className="text-h3">{template.title}</h3>
        )}
        {canEdit && (
          <Button variant="destructive" size="sm" onClick={handleDelete}>
            Delete
          </Button>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor={`template-description-${template.id}`}>Description</Label>
        {canEdit ? (
          <Input
            id={`template-description-${template.id}`}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            onBlur={() =>
              description !== (template.description ?? "") &&
              patch({ description: parseOptionalText(description) })
            }
          />
        ) : (
          <p className="text-sm text-muted-foreground">{template.description ?? "—"}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label>Applicable categories</Label>
        <p className="text-xs text-muted-foreground">None selected means applicable to any category.</p>
        <div className="flex flex-wrap gap-1">
          {ALL_CATEGORIES.map((category) => {
            const active = template.applicableCategories.includes(category);
            return (
              <button
                key={category}
                type="button"
                disabled={!canEdit}
                onClick={() => toggleCategory(category)}
                className={
                  active
                    ? "rounded-full bg-primary px-2 py-0.5 text-xs text-primary-foreground"
                    : "rounded-full border border-border px-2 py-0.5 text-xs text-muted-foreground"
                }
              >
                {category}
              </button>
            );
          })}
        </div>
      </div>

      <TemplateStepList
        steps={template.steps}
        canEdit={canEdit}
        onAdd={(request) =>
          mutations.createStep.mutate(
            { templateId: template.id, request },
            { onError: () => toast.error("Couldn't add step.") }
          )
        }
        onEdit={(stepId, request) =>
          mutations.patchStep.mutate(
            { templateId: template.id, stepId, request },
            { onError: () => toast.error("Couldn't update step.") }
          )
        }
        onDelete={(stepId) =>
          mutations.deleteStep.mutate(
            { templateId: template.id, stepId },
            { onError: () => toast.error("Couldn't delete step.") }
          )
        }
        onReorder={(stepIds) =>
          mutations.reorderSteps.mutate(
            { templateId: template.id, stepIds },
            { onError: () => toast.error("Couldn't reorder steps.") }
          )
        }
      />
    </div>
  );
}
