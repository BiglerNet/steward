import { Plus } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { AssetFormDialog } from "@/components/assets/AssetFormDialog";
import { Button } from "@/components/ui/button";
import { useAssets } from "@/hooks/useAssets";
import type { AssetType } from "@/api/types";
import { ASSET_TYPE_LABELS, assetTypeIconColors } from "@/lib/assetTypeFieldConfig";
import { cn } from "@/lib/utils";
import { useHouseholdRole } from "@/lib/permissions";

const ASSET_TYPES = Object.keys(ASSET_TYPE_LABELS) as AssetType[];

export function AssetListPage() {
  const { householdId } = useParams() as { householdId: string };
  const { canEdit } = useHouseholdRole();
  const [typeFilter, setTypeFilter] = useState<string>("all");

  const { data: assets } = useAssets(
    householdId,
    typeFilter === "all" ? undefined : (typeFilter as AssetType)
  );

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-h1">My Gear</h1>
        {canEdit && (
          <AssetFormDialog householdId={householdId} trigger={<Button>Add asset</Button>} />
        )}
      </div>

      <div className="flex flex-wrap gap-1.5">
        <button
          type="button"
          onClick={() => setTypeFilter("all")}
          className={cn(
            "rounded-full border border-border bg-card px-3.5 py-1.5 text-small font-medium text-muted-foreground transition-colors hover:border-primary hover:text-primary",
            typeFilter === "all" && "border-primary bg-primary text-primary-foreground hover:text-primary-foreground"
          )}
        >
          All
        </button>
        {ASSET_TYPES.map((type) => (
          <button
            key={type}
            type="button"
            onClick={() => setTypeFilter(type)}
            className={cn(
              "rounded-full border border-border bg-card px-3.5 py-1.5 text-small font-medium text-muted-foreground transition-colors hover:border-primary hover:text-primary",
              typeFilter === type &&
                "border-primary bg-primary text-primary-foreground hover:text-primary-foreground"
            )}
          >
            {ASSET_TYPE_LABELS[type]}
          </button>
        ))}
      </div>

      {assets && assets.length === 0 ? (
        <div className="flex flex-col items-center gap-2 rounded-lg border-2 border-dashed border-border py-16 text-center">
          <p className="text-body text-muted-foreground">No assets yet.</p>
          {canEdit && (
            <p className="text-small text-muted-foreground">Add your first asset to get started.</p>
          )}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {assets?.map((asset) => (
            <Link
              key={asset.id}
              to={`/households/${householdId}/assets/${asset.id}`}
              className="flex flex-col rounded-lg border border-border bg-card p-5 transition-[transform,box-shadow] hover:-translate-y-0.5 hover:shadow-[0_4px_16px_rgba(0,0,0,0.08)]"
            >
              <div className="mb-3.5 flex items-start gap-3">
                <div
                  className="flex h-11 w-11 shrink-0 items-center justify-center rounded-[10px] text-h3"
                  style={{ backgroundColor: assetTypeIconColors[asset.assetType] }}
                >
                  {ASSET_TYPE_LABELS[asset.assetType][0]}
                </div>
                <div>
                  <p className="text-h3">{asset.name}</p>
                  <p className="text-small text-muted-foreground">
                    {ASSET_TYPE_LABELS[asset.assetType]}
                    {asset.year ? ` · ${asset.year}` : ""}
                  </p>
                </div>
              </div>
              {asset.photoUrl && (
                <img
                  src={asset.photoUrl}
                  alt={asset.name}
                  className="h-32 w-full rounded-md object-cover"
                />
              )}
            </Link>
          ))}
          {canEdit && (
            <AssetFormDialog
              householdId={householdId}
              trigger={
                <button
                  type="button"
                  className="flex min-h-[200px] flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed border-border text-muted-foreground transition-colors hover:border-primary hover:text-primary"
                >
                  <Plus className="h-6 w-6" />
                  <span className="text-body font-semibold">Add Asset</span>
                </button>
              }
            />
          )}
        </div>
      )}
    </div>
  );
}
