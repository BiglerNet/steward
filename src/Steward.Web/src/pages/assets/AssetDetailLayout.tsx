import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { NavLink, Outlet, useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { deleteAsset, getAsset } from "@/api/assets";
import { AssetFormDialog } from "@/components/assets/AssetFormDialog";
import { Button } from "@/components/ui/button";
import { assetTypeFieldConfig, ASSET_TYPE_LABELS } from "@/lib/assetTypeFieldConfig";
import { cn } from "@/lib/utils";
import { useHouseholdRole } from "@/lib/permissions";

const TABS = [
  { to: "engines", label: "Engines" },
  { to: "service-records", label: "Service Records" },
  { to: "mileage-logs", label: "Mileage Logs" },
  { to: "engine-hours-logs", label: "Engine Hours Logs" },
  { to: "fuel-logs", label: "Fuel Logs" },
  { to: "registrations", label: "Registrations" },
  { to: "warranties", label: "Warranties" },
];

export function AssetDetailLayout() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { canEdit, canDeleteStructural } = useHouseholdRole();

  const { data: asset } = useQuery({
    queryKey: ["households", householdId, "assets", assetId],
    queryFn: () => getAsset(householdId, assetId),
  });

  const deleteMutation = useMutation({
    mutationFn: () => deleteAsset(householdId, assetId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets"] });
      navigate(`/households/${householdId}/assets`);
    },
    onError: () => toast.error("Couldn't delete this asset."),
  });

  function handleDelete() {
    if (asset && window.confirm(`Delete "${asset.name}"? This can't be undone.`)) {
      deleteMutation.mutate();
    }
  }

  if (!asset) {
    return null;
  }

  const typeFields = assetTypeFieldConfig[asset.assetType].filter(
    (typeField) => asset[typeField.key] !== null && asset[typeField.key] !== undefined
  );

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-h1">{asset.name}</h1>
          <p className="text-body text-muted-foreground">
            {ASSET_TYPE_LABELS[asset.assetType]}
            {asset.year ? ` · ${asset.year}` : ""}
          </p>
        </div>
        <div className="flex gap-2">
          {canEdit && (
            <AssetFormDialog
              householdId={householdId}
              asset={asset}
              trigger={<Button variant="outline">Edit</Button>}
            />
          )}
          {canDeleteStructural && (
            <Button variant="destructive" onClick={handleDelete} disabled={deleteMutation.isPending}>
              Delete
            </Button>
          )}
        </div>
      </div>

      {(asset.description || typeFields.length > 0) && (
        <div className="overflow-hidden rounded-lg border border-border bg-card">
          <div className="border-b border-border bg-background px-5 py-3.5 text-h3">
            Asset Details
          </div>
          <div className="px-5 py-4">
            {asset.description && <p className="text-body">{asset.description}</p>}
            {typeFields.length > 0 && (
              <dl className={cn("grid grid-cols-2 gap-x-4 sm:grid-cols-3", asset.description && "mt-3")}>
                {typeFields.map((typeField) => (
                  <div
                    key={typeField.key}
                    className="flex justify-between border-b border-background py-2 text-body last:border-b-0 sm:flex-col sm:justify-start sm:border-b-0 sm:py-1"
                  >
                    <dt className="text-muted-foreground">{typeField.label}</dt>
                    <dd className="font-medium">{String(asset[typeField.key])}</dd>
                  </div>
                ))}
              </dl>
            )}
          </div>
        </div>
      )}

      <nav className="flex gap-0 overflow-x-auto border-b border-border">
        {TABS.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            className={({ isActive }) =>
              cn(
                "whitespace-nowrap border-b-2 border-transparent px-5 py-3 text-body text-muted-foreground transition-colors hover:text-foreground",
                isActive && "border-primary text-primary"
              )
            }
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>

      <Outlet />
    </div>
  );
}
