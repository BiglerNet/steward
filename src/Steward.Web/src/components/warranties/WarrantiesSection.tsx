import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useParams } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import { createWarranty, deleteWarranty, listWarranties, updateWarranty } from "@/api/warranties";
import type { WarrantyResponse } from "@/api/types";
import { DocumentAttachment } from "@/components/documents/DocumentAttachment";
import { ExpiryBadge } from "@/components/documents/ExpiryBadge";
import { MarkdownContent } from "@/components/markdown/MarkdownContent";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";
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
import { parseOptionalText, textToInputValue } from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";

const schema = z.object({
  provider: z.string().min(1, "Provider is required"),
  description: z.string().optional(),
  startsOn: z.string().optional(),
  expiresOn: z.string().optional(),
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  provider: "",
  description: "",
  startsOn: "",
  expiresOn: "",
  notes: "",
};

export function WarrantiesSection() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<WarrantyResponse | null>(null);

  const queryKey = ["households", householdId, "assets", assetId, "warranties"];
  const { data: warranties } = useQuery({
    queryKey,
    queryFn: () => listWarranties(householdId, assetId),
  });
  const sorted = [...(warranties ?? [])].sort((a, b) =>
    (b.expiresOn ?? "").localeCompare(a.expiresOn ?? "")
  );

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
  }

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload = {
        provider: values.provider,
        description: parseOptionalText(values.description),
        startsOn: parseOptionalText(values.startsOn),
        expiresOn: parseOptionalText(values.expiresOn),
        notes: parseOptionalText(values.notes),
      };
      return editing
        ? updateWarranty(householdId, assetId, editing.id, payload)
        : createWarranty(householdId, assetId, payload);
    },
    onSuccess: () => {
      invalidate();
      setOpen(false);
      setEditing(null);
      form.reset(defaultValues);
      toast.success(editing ? "Warranty updated." : "Warranty added.");
    },
    onError: () => toast.error("Couldn't save this warranty. Please try again."),
  });

  const deleteMutation = useMutation({
    mutationFn: (warranty: WarrantyResponse) => deleteWarranty(householdId, assetId, warranty.id),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't delete this warranty."),
  });

  function openCreate() {
    setEditing(null);
    form.reset(defaultValues);
    setOpen(true);
  }

  function openEdit(warranty: WarrantyResponse) {
    setEditing(warranty);
    form.reset({
      provider: warranty.provider,
      description: textToInputValue(warranty.description),
      startsOn: textToInputValue(warranty.startsOn),
      expiresOn: textToInputValue(warranty.expiresOn),
      notes: textToInputValue(warranty.notes),
    });
    setOpen(true);
  }

  function handleDelete(warranty: WarrantyResponse) {
    if (window.confirm("Delete this warranty? This can't be undone.")) {
      deleteMutation.mutate(warranty);
    }
  }

  function documentUrlFor(warranty: WarrantyResponse) {
    return `/api/households/${householdId}/assets/${assetId}/warranties/${warranty.id}/document`;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">Warranties</h2>
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
                <DialogTitle>{editing ? "Edit warranty" : "Add warranty"}</DialogTitle>
              </DialogHeader>
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
                  className="space-y-4"
                >
                  <FormField
                    control={form.control}
                    name="provider"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Provider</FormLabel>
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
                          <MarkdownEditor value={field.value} onChange={field.onChange} onBlur={field.onBlur} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="startsOn"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Starts on</FormLabel>
                        <FormControl>
                          <Input type="date" {...field} />
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
        <p className="text-sm text-muted-foreground">No warranties yet. Add the first one.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Provider</TableHead>
              <TableHead>Description</TableHead>
              <TableHead>Starts on</TableHead>
              <TableHead>Expires on</TableHead>
              <TableHead>Document</TableHead>
              {canEdit && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {sorted.map((warranty) => (
              <TableRow key={warranty.id}>
                <TableCell>{warranty.provider}</TableCell>
                <TableCell>
                  {warranty.description ? <MarkdownContent>{warranty.description}</MarkdownContent> : "—"}
                </TableCell>
                <TableCell>{warranty.startsOn ?? "—"}</TableCell>
                <TableCell className="space-x-2">
                  <span>{warranty.expiresOn ?? "—"}</span>
                  <ExpiryBadge expiresOn={warranty.expiresOn} />
                </TableCell>
                <TableCell>
                  <DocumentAttachment
                    hasDocument={warranty.hasDocument}
                    uploadUrl={documentUrlFor(warranty)}
                    downloadUrl={documentUrlFor(warranty)}
                    deleteUrl={documentUrlFor(warranty)}
                    canEdit={canEdit}
                    onChange={invalidate}
                  />
                </TableCell>
                {canEdit && (
                  <TableCell className="text-right space-x-2">
                    <Button size="sm" variant="outline" onClick={() => openEdit(warranty)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={deleteMutation.isPending}
                      onClick={() => handleDelete(warranty)}
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
