import type { Control, FieldPath } from "react-hook-form";
import type { AssetTypeDefinition } from "@/api/types";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { type AssetFieldsFormValues, fieldsFor } from "@/lib/assetTypes";

const USAGE_TRACKING_MODES: AssetFieldsFormValues["usageTrackingMode"][] = [
  "None",
  "Mileage",
  "Hours",
  "Both",
];

interface AssetFieldsSectionProps<TFieldValues extends AssetFieldsFormValues> {
  control: Control<TFieldValues>;
  definition: AssetTypeDefinition;
}

/// Shared by the edit dialog and the wizard's Details step. TFieldValues lets each
/// caller extend the base shape (the dialog adds `category`) while field paths still
/// type-check against react-hook-form.
export function AssetFieldsSection<TFieldValues extends AssetFieldsFormValues>({
  control,
  definition,
}: AssetFieldsSectionProps<TFieldValues>) {
  return (
    <>
      <FormField
        control={control}
        name={"name" as FieldPath<TFieldValues>}
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
        control={control}
        name={"description" as FieldPath<TFieldValues>}
        render={({ field }) => (
          <FormItem>
            <FormLabel>Description</FormLabel>
            <FormControl>
              <MarkdownEditor value={field.value ?? ""} onChange={field.onChange} onBlur={field.onBlur} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <FormField
        control={control}
        name={"year" as FieldPath<TFieldValues>}
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
        control={control}
        name={"usageTrackingMode" as FieldPath<TFieldValues>}
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

      {fieldsFor(definition).map((typeField) => (
        <FormField
          key={typeField.key}
          control={control}
          name={typeField.key as FieldPath<TFieldValues>}
          render={({ field }) => (
            <FormItem>
              <FormLabel>{typeField.label}</FormLabel>
              {typeField.kind === "select" ? (
                <Select value={field.value ?? ""} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Not set" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="">Not set</SelectItem>
                    {typeField.options?.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ) : (
                <FormControl>
                  <Input type={typeField.kind === "number" ? "number" : "text"} {...field} />
                </FormControl>
              )}
              <FormMessage />
            </FormItem>
          )}
        />
      ))}
    </>
  );
}
