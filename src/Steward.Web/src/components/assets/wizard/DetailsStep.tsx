import type { UseFormReturn } from "react-hook-form";
import type { AssetTypeDefinition } from "@/api/types";
import { AssetFieldsSection } from "@/components/assets/AssetFieldsSection";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import type { AssetFieldsFormValues } from "@/lib/assetTypes";

interface DetailsStepProps {
  form: UseFormReturn<AssetFieldsFormValues>;
  definition: AssetTypeDefinition;
  vinDecodeSummary: string | null;
  vinDecodeFailed: boolean;
  mismatchHint: string | null;
  onBack: () => void;
  onSubmit: (values: AssetFieldsFormValues) => void;
  submitLabel: string;
  submitting: boolean;
}

export function DetailsStep({
  form,
  definition,
  vinDecodeSummary,
  vinDecodeFailed,
  mismatchHint,
  onBack,
  onSubmit,
  submitLabel,
  submitting,
}: DetailsStepProps) {
  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {vinDecodeSummary && (
          <p className="rounded-md border border-border bg-muted px-3 py-2 text-small text-muted-foreground">
            Found: {vinDecodeSummary}. Fields were prefilled — you can change anything.
          </p>
        )}
        {vinDecodeFailed && (
          <p className="rounded-md border border-border bg-muted px-3 py-2 text-small text-muted-foreground">
            Couldn't decode this VIN — enter details manually.
          </p>
        )}
        {mismatchHint && (
          <p className="rounded-md border border-border bg-muted px-3 py-2 text-small text-muted-foreground">
            {mismatchHint}
          </p>
        )}
        <AssetFieldsSection control={form.control} definition={definition} />
        <div className="flex justify-between">
          <Button type="button" variant="outline" onClick={onBack}>
            Back
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? "Saving…" : submitLabel}
          </Button>
        </div>
      </form>
    </Form>
  );
}
