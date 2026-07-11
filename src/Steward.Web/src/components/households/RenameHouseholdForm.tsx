import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { updateHousehold } from "@/api/households";
import type { HouseholdResponse } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";

const schema = z.object({
  name: z.string().min(1, "Household name is required"),
});

type FormValues = z.infer<typeof schema>;

interface RenameHouseholdFormProps {
  household: HouseholdResponse;
  canEdit: boolean;
}

export function RenameHouseholdForm({ household, canEdit }: RenameHouseholdFormProps) {
  const queryClient = useQueryClient();
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: household.name },
  });

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      updateHousehold(household.id, {
        name: values.name,
        publicSlug: household.publicSlug,
        isPublicVisible: household.isPublicVisible,
        country: household.country,
        region: household.region,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households"] });
      toast.success("Household renamed.");
    },
    onError: () => {
      toast.error("Couldn't rename the household.");
    },
  });

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
        className="flex items-end gap-3"
      >
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem className="flex-1">
              <FormLabel>Household name</FormLabel>
              <FormControl>
                <Input disabled={!canEdit} {...field} />
              </FormControl>
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
