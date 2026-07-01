import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  useForm,
  type DefaultValues,
  type FieldValues,
  type Resolver,
  type UseFormReturn,
} from "react-hook-form";
import { toast } from "sonner";
import type { ZodType } from "zod";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export interface TrackingLogColumn<TRecord> {
  key: string;
  header: string;
  render: (record: TRecord) => React.ReactNode;
}

export interface TrackingLogSectionProps<TRecord, TFormValues extends FieldValues> {
  title: string;
  emptyMessage: string;
  queryKey: unknown[];
  columns: TrackingLogColumn<TRecord>[];
  list: () => Promise<TRecord[]>;
  create: (values: TFormValues) => Promise<TRecord>;
  update: (id: string, values: TFormValues) => Promise<TRecord>;
  remove: (id: string) => Promise<void>;
  getId: (record: TRecord) => string;
  sortValue: (record: TRecord) => string;
  schema: ZodType<TFormValues>;
  defaultValues: TFormValues;
  toFormValues: (record: TRecord) => TFormValues;
  renderFields: (form: UseFormReturn<TFormValues>) => React.ReactNode;
  canEdit: boolean;
}

export function TrackingLogSection<TRecord, TFormValues extends FieldValues>({
  title,
  emptyMessage,
  queryKey,
  columns,
  list,
  create,
  update,
  remove,
  getId,
  sortValue,
  schema,
  defaultValues,
  toFormValues,
  renderFields,
  canEdit,
}: TrackingLogSectionProps<TRecord, TFormValues>) {
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<TRecord | null>(null);
  const queryClient = useQueryClient();

  const { data: records } = useQuery({ queryKey, queryFn: list });
  const sorted = [...(records ?? [])].sort((a, b) => (sortValue(a) < sortValue(b) ? 1 : -1));

  const form = useForm<TFormValues, unknown, TFormValues>({
    resolver: zodResolver(schema) as Resolver<TFormValues, unknown, TFormValues>,
    defaultValues: defaultValues as DefaultValues<TFormValues>,
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
  }

  const saveMutation = useMutation({
    mutationFn: (values: TFormValues) =>
      editing ? update(getId(editing), values) : create(values),
    onSuccess: () => {
      invalidate();
      setOpen(false);
      setEditing(null);
      form.reset(defaultValues);
      toast.success(editing ? "Entry updated." : "Entry added.");
    },
    onError: () => {
      toast.error("Couldn't save this entry. Please try again.");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (record: TRecord) => remove(getId(record)),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't delete this entry."),
  });

  function openCreate() {
    setEditing(null);
    form.reset(defaultValues);
    setOpen(true);
  }

  function openEdit(record: TRecord) {
    setEditing(record);
    form.reset(toFormValues(record));
    setOpen(true);
  }

  function handleDelete(record: TRecord) {
    if (window.confirm("Delete this entry? This can't be undone.")) {
      deleteMutation.mutate(record);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">{title}</h2>
        {canEdit && (
          <Dialog
            open={open}
            onOpenChange={(next) => {
              setOpen(next);
              if (!next) {
                setEditing(null);
              }
            }}
          >
            <DialogTrigger asChild>
              <Button size="sm" onClick={openCreate}>
                Add entry
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{editing ? "Edit entry" : `Add ${title.toLowerCase()} entry`}</DialogTitle>
              </DialogHeader>
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
                  className="space-y-4"
                >
                  {renderFields(form)}
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
        <p className="text-sm text-muted-foreground">{emptyMessage}</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              {columns.map((column) => (
                <TableHead key={column.key}>{column.header}</TableHead>
              ))}
              {canEdit && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {sorted.map((record) => (
              <TableRow key={getId(record)}>
                {columns.map((column) => (
                  <TableCell key={column.key}>{column.render(record)}</TableCell>
                ))}
                {canEdit && (
                  <TableCell className="text-right space-x-2">
                    <Button size="sm" variant="outline" onClick={() => openEdit(record)}>
                      Edit
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={deleteMutation.isPending}
                      onClick={() => handleDelete(record)}
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
