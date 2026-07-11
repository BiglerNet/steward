import type { AssetCategory, AssetTypeDefinition } from "@/api/types";
import { AssetTypeIcon } from "@/components/assets/AssetTypeIcon";
import { Button } from "@/components/ui/button";
import { ASSET_GROUP_LABELS, ASSET_GROUP_ORDER } from "@/lib/assetTypes";
import { cn } from "@/lib/utils";

interface TypeStepProps {
  registry: AssetTypeDefinition[];
  selectedCategory: AssetCategory | undefined;
  onSelect: (category: AssetCategory) => void;
  onContinue: () => void;
}

export function TypeStep({ registry, selectedCategory, onSelect, onContinue }: TypeStepProps) {
  return (
    <div className="space-y-5">
      <div role="radiogroup" aria-label="Asset type" className="space-y-5">
        {ASSET_GROUP_ORDER.map((group) => {
          const definitions = registry.filter((d) => d.group === group);
          if (definitions.length === 0) {
            return null;
          }
          return (
            <div key={group} className="space-y-2">
              <h3 className="text-h3">{ASSET_GROUP_LABELS[group]}</h3>
              <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                {definitions.map((definition) => (
                  <button
                    key={definition.category}
                    type="button"
                    role="radio"
                    aria-checked={selectedCategory === definition.category}
                    onClick={() => onSelect(definition.category)}
                    className={cn(
                      "flex items-center gap-3 rounded-lg border-2 px-3 py-2 text-left transition-colors",
                      selectedCategory === definition.category
                        ? "border-primary bg-primary/5"
                        : "border-border hover:border-primary"
                    )}
                  >
                    <AssetTypeIcon icon={definition.icon} group={definition.group} size="sm" />
                    <span className="text-body font-medium">{definition.displayLabel}</span>
                  </button>
                ))}
              </div>
            </div>
          );
        })}
      </div>

      <div className="flex justify-end">
        <Button type="button" disabled={!selectedCategory} onClick={onContinue}>
          Continue
        </Button>
      </div>
    </div>
  );
}
