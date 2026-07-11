import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { updateAsset } from "@/api/assets";
import type { AssetResponse, AssetTypeDefinition } from "@/api/types";
import { AssetFieldsSection } from "@/components/assets/AssetFieldsSection";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Form } from "@/components/ui/form";
import { Label } from "@/components/ui/label";
import { useAssetTypeRegistry } from "@/hooks/useAssetTypeRegistry";
import {
  type AssetFieldsFormValues,
  assetFieldsSchema,
  assetToAssetFieldsValues,
  clearInapplicableFields,
  findDefinition,
} from "@/lib/assetTypes";
import { parseOptionalNumber, parseOptionalText } from "@/lib/formHelpers";

interface AssetFormDialogProps {
  householdId: string;
  asset: AssetResponse;
  trigger: React.ReactNode;
}

export function AssetFormDialog({ householdId, asset, trigger }: AssetFormDialogProps) {
  const [open, setOpen] = useState(false);
  const { data: registry, isError, refetch } = useAssetTypeRegistry();

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit asset</DialogTitle>
        </DialogHeader>
        {registry ? (
          <AssetForm
            householdId={householdId}
            asset={asset}
            registry={registry}
            onSaved={() => setOpen(false)}
          />
        ) : isError ? (
          <div className="flex flex-col items-center gap-3 py-6">
            <p className="text-body text-muted-foreground">Couldn't load asset types.</p>
            <Button variant="outline" onClick={() => refetch()}>
              Retry
            </Button>
          </div>
        ) : (
          <p className="py-6 text-center text-body text-muted-foreground">Loading…</p>
        )}
      </DialogContent>
    </Dialog>
  );
}

interface AssetFormProps {
  householdId: string;
  asset: AssetResponse;
  registry: AssetTypeDefinition[];
  onSaved: () => void;
}

function AssetForm({ householdId, asset, registry, onSaved }: AssetFormProps) {
  const queryClient = useQueryClient();
  const definition = findDefinition(registry, asset.category);

  const form = useForm<AssetFieldsFormValues>({
    resolver: zodResolver(assetFieldsSchema),
    defaultValues: assetToAssetFieldsValues(asset),
  });

  const mutation = useMutation({
    mutationFn: (values: AssetFieldsFormValues) => {
      const payload = {
        category: asset.category,
        name: values.name,
        description: parseOptionalText(values.description),
        year: parseOptionalNumber(values.year),
        usageTrackingMode: values.usageTrackingMode,
        ...clearInapplicableFields(definition!, values),
      };
      return updateAsset(householdId, asset.id, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets"] });
      onSaved();
      toast.success("Asset updated.");
    },
    onError: () => toast.error("Couldn't save this asset. Please try again."),
  });

  if (!definition) {
    return null;
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        className="space-y-4"
      >
        <div className="space-y-2">
          <Label>Category</Label>
          <p className="text-body text-muted-foreground">{definition.displayLabel}</p>
        </div>
        <AssetFieldsSection control={form.control} definition={definition} />

        <DialogFooter>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? "Saving…" : "Save"}
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
}
