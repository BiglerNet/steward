import { useMemo, useState } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import { useHouseholdTemplates, usePlatformTemplates } from "@/hooks/useTemplates";
import { isTemplateApplicable } from "@/lib/templates";
import type { AssetCategory, TemplateResponse } from "@/api/types";

interface TemplatePickerProps {
  householdId: string;
  assetCategory: AssetCategory;
  value: string | null;
  onChange: (templateId: string | null, template: TemplateResponse | null) => void;
}

export function TemplatePicker({ householdId, assetCategory, value, onChange }: TemplatePickerProps) {
  const [search, setSearch] = useState("");
  const { data: householdTemplates } = useHouseholdTemplates(householdId, assetCategory);
  const { data: platformTemplates } = usePlatformTemplates(assetCategory);

  const candidates = useMemo(() => {
    const all = [...(householdTemplates ?? []), ...(platformTemplates ?? [])];
    return all
      .filter((t) => isTemplateApplicable(t, assetCategory))
      .filter((t) => t.title.toLowerCase().includes(search.toLowerCase()));
  }, [householdTemplates, platformTemplates, assetCategory, search]);

  const selected = candidates.find((t) => t.id === value) ?? null;

  return (
    <div className="space-y-2">
      <Input
        placeholder="Search templates…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <div className="max-h-40 overflow-y-auto rounded-md border border-border">
        <button
          type="button"
          onClick={() => onChange(null, null)}
          className={cn(
            "flex w-full items-center px-3 py-2 text-left text-sm hover:bg-accent",
            value === null && "bg-accent"
          )}
        >
          No template — start from scratch
        </button>
        {candidates.length === 0 && (
          <p className="px-3 py-2 text-sm text-muted-foreground">No matching templates.</p>
        )}
        {candidates.map((template) => (
          <button
            key={template.id}
            type="button"
            onClick={() => onChange(template.id, template)}
            className={cn(
              "flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-accent",
              value === template.id && "bg-accent"
            )}
          >
            <span>{template.title}</span>
            <span className="text-xs text-muted-foreground">
              {template.steps.length} step{template.steps.length === 1 ? "" : "s"}
              {template.householdId === null ? " · Platform" : ""}
            </span>
          </button>
        ))}
      </div>
      {selected && (
        <div className="rounded-md border border-border bg-muted/40 px-3 py-2 text-sm">
          <p className="font-medium">{selected.title}</p>
          <ul className="mt-1 list-inside list-disc text-muted-foreground">
            {selected.steps.map((step) => (
              <li key={step.id}>{step.text}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
