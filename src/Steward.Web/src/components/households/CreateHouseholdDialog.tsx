import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import { createHousehold } from "@/api/households";
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
import { slugify } from "@/lib/slug";
import { writeLastHouseholdId } from "@/lib/session";

const schema = z.object({
  name: z.string().min(1, "Household name is required"),
});

type FormValues = z.infer<typeof schema>;

interface CreateHouseholdDialogProps {
  trigger: React.ReactNode;
}

export function CreateHouseholdDialog({ trigger }: CreateHouseholdDialogProps) {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: "" },
  });

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      createHousehold({
        name: values.name,
        publicSlug: slugify(values.name),
        isPublicVisible: false,
        country: null,
        region: null,
      }),
    onSuccess: (household) => {
      queryClient.invalidateQueries({ queryKey: ["households"] });
      writeLastHouseholdId(household.id);
      setOpen(false);
      form.reset();
      navigate(`/households/${household.id}`);
    },
    onError: () => {
      toast.error("Couldn't create the household. Please try again.");
    },
  });

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create household</DialogTitle>
        </DialogHeader>
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
            className="space-y-4"
          >
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Household name</FormLabel>
                  <FormControl>
                    <Input placeholder="The Smith Garage" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? "Creating…" : "Create household"}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
