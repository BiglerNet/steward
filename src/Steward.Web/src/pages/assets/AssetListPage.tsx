import { Plus } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router";
import { AssetCoverThumbnail } from "@/components/assets/AssetCoverThumbnail";
import { AssetTypeIcon } from "@/components/assets/AssetTypeIcon";
import { Button } from "@/components/ui/button";
import { useAssets } from "@/hooks/useAssets";
import { useAssetTypeRegistry } from "@/hooks/useAssetTypeRegistry";
import { findDefinition } from "@/lib/assetTypes";
import { cn } from "@/lib/utils";
import { useHouseholdRole } from "@/lib/permissions";

export function AssetListPage() {
  const { householdId } = useParams() as { householdId: string };
  const { canEdit } = useHouseholdRole();
  const [categoryFilter, setCategoryFilter] = useState<string>("all");
  const { data: registry, isError, refetch } = useAssetTypeRegistry();

  const { data: allAssets } = useAssets(householdId);
  const assets =
    categoryFilter === "all"
      ? allAssets
      : allAssets?.filter((asset) => asset.category === categoryFilter);

  if (isError) {
    return (
      <div className="flex flex-col items-center gap-3 rounded-lg border-2 border-dashed border-border py-16 text-center">
        <p className="text-body text-muted-foreground">Couldn't load asset types.</p>
        <Button variant="outline" onClick={() => refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  if (!registry) {
    return null;
  }

  // Only offer filters for categories the household actually has.
  const presentCategories = new Set(allAssets?.map((asset) => asset.category) ?? []);
  const filterableDefinitions = registry.filter((definition) =>
    presentCategories.has(definition.category)
  );

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-h1">My Gear</h1>
        {canEdit && (
          <Button asChild>
            <Link to={`/households/${householdId}/assets/new`}>Add asset</Link>
          </Button>
        )}
      </div>

      <div className="flex flex-wrap gap-1.5">
        <button
          type="button"
          onClick={() => setCategoryFilter("all")}
          className={cn(
            "rounded-full border border-border bg-card px-3.5 py-1.5 text-small font-medium text-muted-foreground transition-colors hover:border-primary hover:text-primary",
            categoryFilter === "all" && "border-primary bg-primary text-primary-foreground hover:text-primary-foreground"
          )}
        >
          All
        </button>
        {filterableDefinitions.map((definition) => (
          <button
            key={definition.category}
            type="button"
            onClick={() => setCategoryFilter(definition.category)}
            className={cn(
              "rounded-full border border-border bg-card px-3.5 py-1.5 text-small font-medium text-muted-foreground transition-colors hover:border-primary hover:text-primary",
              categoryFilter === definition.category &&
                "border-primary bg-primary text-primary-foreground hover:text-primary-foreground"
            )}
          >
            {definition.displayLabel}
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
          {assets?.map((asset) => {
            const definition = findDefinition(registry, asset.category);
            const label = definition?.displayLabel ?? asset.category;
            return (
              <Link
                key={asset.id}
                to={`/households/${householdId}/assets/${asset.id}`}
                className="flex flex-col rounded-lg border border-border bg-card p-5 transition-[transform,box-shadow] hover:-translate-y-0.5 hover:shadow-[0_4px_16px_rgba(0,0,0,0.08)]"
              >
                <div className="mb-3.5 flex items-start gap-3">
                  {definition && <AssetTypeIcon icon={definition.icon} group={definition.group} />}
                  <div>
                    <p className="text-h3">{asset.name}</p>
                    <p className="text-small text-muted-foreground">
                      {label}
                      {asset.year ? ` · ${asset.year}` : ""}
                    </p>
                  </div>
                </div>
                {asset.coverPhotoId && (
                  <AssetCoverThumbnail
                    householdId={householdId}
                    assetId={asset.id}
                    coverPhotoId={asset.coverPhotoId}
                    alt={asset.name}
                    className="h-32 w-full rounded-md object-cover"
                  />
                )}
              </Link>
            );
          })}
          {canEdit && (
            <Link
              to={`/households/${householdId}/assets/new`}
              className="flex min-h-[200px] flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed border-border text-muted-foreground transition-colors hover:border-primary hover:text-primary"
            >
              <Plus className="h-6 w-6" />
              <span className="text-body font-semibold">Add Asset</span>
            </Link>
          )}
        </div>
      )}
    </div>
  );
}
