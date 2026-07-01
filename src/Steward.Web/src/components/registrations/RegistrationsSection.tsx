import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useParams } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import {
  createRegistration,
  deleteRegistration,
  listRegistrations,
  updateRegistration,
} from "@/api/registrations";
import type { RegistrationResponse } from "@/api/types";
import { DocumentAttachment } from "@/components/documents/DocumentAttachment";
import { ExpiryBadge } from "@/components/documents/ExpiryBadge";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  numberToInputValue,
  optionalNumberString,
  parseOptionalNumber,
  parseOptionalText,
  textToInputValue,
} from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";

const schema = z.object({
  registrationNumber: z.string().min(1, "Registration number is required"),
  issuingAuthority: z.string().optional(),
  renewedOn: z.string().optional(),
  cost: optionalNumberString,
  expiresOn: z.string().optional(),
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  registrationNumber: "",
  issuingAuthority: "",
  renewedOn: "",
  cost: "",
  expiresOn: "",
  notes: "",
};

export function RegistrationsSection() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<RegistrationResponse | null>(null);

  const queryKey = ["households", householdId, "assets", assetId, "registrations"];
  const { data: registrations } = useQuery({
    queryKey,
    queryFn: () => listRegistrations(householdId, assetId),
  });
  const sorted = [...(registrations ?? [])].sort((a, b) =>
    (b.expiresOn ?? "").localeCompare(a.expiresOn ?? "")
  );

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
  }

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload = {
        registrationNumber: values.registrationNumber,
        issuingAuthority: parseOptionalText(values.issuingAuthority),
        renewedOn: parseOptionalText(values.renewedOn),
        cost: parseOptionalNumber(values.cost),
        expiresOn: parseOptionalText(values.expiresOn),
        notes: parseOptionalText(values.notes),
      };
      return editing
        ? updateRegistration(householdId, assetId, editing.id, payload)
        : createRegistration(householdId, assetId, payload);
    },
    onSuccess: () => {
      invalidate();
      setOpen(false);
      setEditing(null);
      form.reset(defaultValues);
      toast.success(editing ? "Registration updated." : "Registration added.");
    },
    onError: () => toast.error("Couldn't save this registration. Please try again."),
  });

  const deleteMutation = useMutation({
    mutationFn: (registration: RegistrationResponse) =>
      deleteRegistration(householdId, assetId, registration.id),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't delete this registration."),
  });

  function openCreate() {
    setEditing(null);
    form.reset(defaultValues);
    setOpen(true);
  }

  function openEdit(registration: RegistrationResponse) {
    setEditing(registration);
    form.reset({
      registrationNumber: registration.registrationNumber,
      issuingAuthority: textToInputValue(registration.issuingAuthority),
      renewedOn: textToInputValue(registration.renewedOn),
      cost: numberToInputValue(registration.cost),
      expiresOn: textToInputValue(registration.expiresOn),
      notes: textToInputValue(registration.notes),
    });
    setOpen(true);
  }

  function handleDelete(registration: RegistrationResponse) {
    if (window.confirm("Delete this registration? This can't be undone.")) {
      deleteMutation.mutate(registration);
    }
  }

  function documentUrlFor(registration: RegistrationResponse) {
    return `/api/households/${householdId}/assets/${assetId}/registrations/${registration.id}/document`;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">Registrations</h2>
        {canEdit && (
          <Dialog
            open={open}
            onOpenChange={(next) => {
              setOpen(next);
              if (!next) setEditing(null);
            }}
          >
            <DialogTrigger asChild>
              <Button size="sm" onClick={openCreate}>
                Add entry
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{editing ? "Edit registration" : "Add registration entry"}</DialogTitle>
              </DialogHeader>
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
                  className="space-y-4"
                >
                  <FormField
                    control={form.control}
                    name="registrationNumber"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Registration number</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="issuingAuthority"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Issuing authority</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="renewedOn"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Renewed on</FormLabel>
                        <FormControl>
                          <Input type="date" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="cost"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Cost</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.01" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="expiresOn"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Expires on</FormLabel>
                        <FormControl>
                          <Input type="date" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="notes"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Notes</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <DialogFooter>
                    <Button type="submit" disabled={saveMutation.isPending}>
                      {saveMutation.isPending ? "Saving…" : "Save"}
                    </Button>
                  </DialogFooter>
                </form>
              </Form>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {sorted.length === 0 ? (
        <p className="text-sm text-muted-foreground">No registrations yet. Log the first renewal.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Registration #</TableHead>
              <TableHead>Issuing authority</TableHead>
              <TableHead>Renewed on</TableHead>
              <TableHead>Cost</TableHead>
              <TableHead>Expires on</TableHead>
              <TableHead>Document</TableHead>
              {canEdit && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {sorted.map((registration) => (
              <TableRow key={registration.id}>
                <TableCell>{registration.registrationNumber}</TableCell>
                <TableCell>{registration.issuingAuthority ?? "—"}</TableCell>
                <TableCell>{registration.renewedOn ?? "—"}</TableCell>
                <TableCell>{registration.cost ?? "—"}</TableCell>
                <TableCell className="space-x-2">
                  <span>{registration.expiresOn ?? "—"}</span>
                  <ExpiryBadge expiresOn={registration.expiresOn} />
                </TableCell>
                <TableCell>
                  <DocumentAttachment
                    hasDocument={registration.hasDocument}
                    uploadUrl={documentUrlFor(registration)}
                    downloadUrl={documentUrlFor(registration)}
                    deleteUrl={documentUrlFor(registration)}
                    canEdit={canEdit}
                    onChange={invalidate}
                  />
                </TableCell>
                {canEdit && (
                  <TableCell className="text-right space-x-2">
                    <Button size="sm" variant="outline" onClick={() => openEdit(registration)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={deleteMutation.isPending}
                      onClick={() => handleDelete(registration)}
                    >
                      Delete
                    </Button>
                  </TableCell>
                )}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
