import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { updateHousehold } from "@/api/households";
import type { HouseholdResponse } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useRegionRegistry } from "@/hooks/useRegionRegistry";

const NONE = "none";

const schema = z.object({
  country: z.string(),
  region: z.string(),
});

type FormValues = z.infer<typeof schema>;

interface HouseholdLocationFormProps {
  household: HouseholdResponse;
  canEdit: boolean;
}

export function HouseholdLocationForm({ household, canEdit }: HouseholdLocationFormProps) {
  const queryClient = useQueryClient();
  const { data: registry } = useRegionRegistry();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      country: household.country ?? NONE,
      region: household.region ?? NONE,
    },
  });

  const selectedCountry = form.watch("country");
  const countryDefinition = registry?.find((country) => country.code === selectedCountry);

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      updateHousehold(household.id, {
        name: household.name,
        publicSlug: household.publicSlug,
        isPublicVisible: household.isPublicVisible,
        country: values.country === NONE ? null : values.country,
        region: values.region === NONE ? null : values.region,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households"] });
      toast.success("Household location updated.");
    },
    onError: () => toast.error("Couldn't update the household location."),
  });

  function handleCountryChange(nextCountry: string) {
    form.setValue("country", nextCountry);
    form.setValue("region", NONE);
  }

  if (!registry) {
    return null;
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        className="flex flex-wrap items-end gap-3"
      >
        <FormField
          control={form.control}
          name="country"
          render={({ field }) => (
            <FormItem className="w-48">
              <FormLabel>Country</FormLabel>
              <Select value={field.value} onValueChange={handleCountryChange} disabled={!canEdit}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value={NONE}>Not set</SelectItem>
                  {registry.map((country) => (
                    <SelectItem key={country.code} value={country.code}>
                      {country.name}
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
          name="region"
          render={({ field }) => (
            <FormItem className="w-48">
              <FormLabel>Region</FormLabel>
              <Select
                value={field.value}
                onValueChange={field.onChange}
                disabled={!canEdit || !countryDefinition}
              >
                <FormControl>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value={NONE}>Not set</SelectItem>
                  {countryDefinition?.regions.map((region) => (
                    <SelectItem key={region.code} value={region.code}>
                      {region.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
        {canEdit && (
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? "Saving…" : "Save"}
          </Button>
        )}
      </form>
    </Form>
  );
}
