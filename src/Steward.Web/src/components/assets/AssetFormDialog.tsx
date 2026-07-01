import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import { createAsset, updateAsset } from "@/api/assets";
import type { AssetResponse, AssetType, UsageTrackingMode } from "@/api/types";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  ALL_ASSET_TYPE_FIELD_KEYS,
  ASSET_TYPE_LABELS,
  assetTypeFieldConfig,
  clearInapplicableFields,
} from "@/lib/assetTypeFieldConfig";
import { numberToInputValue, optionalNumberString, parseOptionalNumber, parseOptionalText, textToInputValue } from "@/lib/formHelpers";

const ASSET_TYPES = Object.keys(ASSET_TYPE_LABELS) as AssetType[];
const USAGE_TRACKING_MODES: UsageTrackingMode[] = ["None", "Mileage", "Hours", "Both"];

const baseFieldsSchema = z.object({
  assetType: z.enum(ASSET_TYPES as [AssetType, ...AssetType[]]),
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
  year: optionalNumberString,
  photoUrl: z.string().optional(),
  usageTrackingMode: z.enum(["None", "Mileage", "Hours", "Both"]),
});

function buildTypeFieldsShape() {
  const shape = {} as Record<(typeof ALL_ASSET_TYPE_FIELD_KEYS)[number], z.ZodOptional<z.ZodString>>;
  for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
    shape[key] = z.string().optional();
  }
  return shape;
}

const typeFieldsSchema = z.object(buildTypeFieldsShape());

const schema = baseFieldsSchema.merge(typeFieldsSchema);

type FormValues = z.infer<typeof schema>;

function emptyTypeFields(): Record<string, string> {
  return Object.fromEntries(ALL_ASSET_TYPE_FIELD_KEYS.map((key) => [key, ""]));
}

function buildDefaultValues(): FormValues {
  return {
    assetType: "Car",
    name: "",
    description: "",
    year: "",
    photoUrl: "",
    usageTrackingMode: "None",
    ...emptyTypeFields(),
  } as FormValues;
}

function assetToFormValues(asset: AssetResponse): FormValues {
  const typeFields = Object.fromEntries(
    ALL_ASSET_TYPE_FIELD_KEYS.map((key) => {
      const value = asset[key];
      return [key, typeof value === "number" ? numberToInputValue(value) : textToInputValue(value)];
    })
  );
  return {
    assetType: asset.assetType,
    name: asset.name,
    description: textToInputValue(asset.description),
    year: numberToInputValue(asset.year),
    photoUrl: textToInputValue(asset.photoUrl),
    usageTrackingMode: asset.usageTrackingMode,
    ...typeFields,
  } as FormValues;
}

interface AssetFormDialogProps {
  householdId: string;
  asset?: AssetResponse;
  trigger: React.ReactNode;
}

export function AssetFormDialog({ householdId, asset, trigger }: AssetFormDialogProps) {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: asset ? assetToFormValues(asset) : buildDefaultValues(),
  });

  const selectedType = form.watch("assetType");

  const mutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload = {
        assetType: values.assetType,
        name: values.name,
        description: parseOptionalText(values.description),
        year: parseOptionalNumber(values.year),
        photoUrl: parseOptionalText(values.photoUrl),
        usageTrackingMode: values.usageTrackingMode,
        ...clearInapplicableFields(values.assetType, values),
      };
      return asset ? updateAsset(householdId, asset.id, payload) : createAsset(householdId, payload);
    },
    onSuccess: (savedAsset) => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets"] });
      setOpen(false);
      if (!asset) {
        form.reset(buildDefaultValues());
        navigate(`/households/${householdId}/assets/${savedAsset.id}`);
      }
      toast.success(asset ? "Asset updated." : "Asset created.");
    },
    onError: () => toast.error("Couldn't save this asset. Please try again."),
  });

  function handleTypeChange(nextType: AssetType) {
    form.setValue("assetType", nextType);
    const applicableKeys = new Set(assetTypeFieldConfig[nextType].map((f) => f.key));
    for (const key of ALL_ASSET_TYPE_FIELD_KEYS) {
      if (!applicableKeys.has(key)) {
        form.setValue(key, "");
      }
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent className="max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{asset ? "Edit asset" : "Add asset"}</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
            className="space-y-4"
          >
            <FormField
              control={form.control}
              name="assetType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Asset type</FormLabel>
                  <Select value={field.value} onValueChange={(value) => handleTypeChange(value as AssetType)}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {ASSET_TYPES.map((type) => (
                        <SelectItem key={type} value={type}>
                          {ASSET_TYPE_LABELS[type]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Description</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="year"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Year</FormLabel>
                  <FormControl>
                    <Input type="number" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="photoUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Photo URL</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="usageTrackingMode"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Usage tracking</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {USAGE_TRACKING_MODES.map((mode) => (
                        <SelectItem key={mode} value={mode}>
                          {mode}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            {assetTypeFieldConfig[selectedType].map((typeField) => (
              <FormField
                key={typeField.key}
                control={form.control}
                name={typeField.key}
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{typeField.label}</FormLabel>
                    <FormControl>
                      <Input type={typeField.kind === "number" ? "number" : "text"} {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            ))}

            <DialogFooter>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? "Saving…" : "Save"}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
