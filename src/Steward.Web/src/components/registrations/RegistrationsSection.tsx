import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useParams } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import { getAsset } from "@/api/assets";
import {
  createRegistration,
  deleteRegistration,
  listRegistrations,
  updateRegistration,
} from "@/api/registrations";
import type { RegistrationKind, RegistrationResponse } from "@/api/types";
import { DocumentAttachment } from "@/components/documents/DocumentAttachment";
import { ExpiryBadge } from "@/components/documents/ExpiryBadge";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";
import { IssuingAuthorityCombobox } from "@/components/registrations/IssuingAuthorityCombobox";
import { RegistrationKindBadge } from "@/components/registrations/RegistrationKindBadge";
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useAssetTypeRegistry } from "@/hooks/useAssetTypeRegistry";
import { useHouseholds } from "@/hooks/useHouseholds";
import { useRegionRegistry } from "@/hooks/useRegionRegistry";
import { findDefinition } from "@/lib/assetTypes";
import {
  numberToInputValue,
  optionalNumberString,
  parseOptionalNumber,
  parseOptionalText,
  textToInputValue,
} from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";
import { computePermitNudges, permitNudgeMessage } from "@/lib/registrationNudges";
import { issuingAuthoritySuggestions } from "@/lib/regions";

const KINDS: RegistrationKind[] = ["Registration", "TrailPass", "Permit"];

const KIND_LABELS: Record<RegistrationKind, string> = {
  Registration: "Registration",
  TrailPass: "Trail pass",
  Permit: "Permit",
};

const NUMBER_LABELS: Record<RegistrationKind, string> = {
  Registration: "Registration #",
  TrailPass: "Pass #",
  Permit: "Permit #",
};

const schema = z.object({
  kind: z.enum(["Registration", "TrailPass", "Permit"]),
  registrationNumber: z.string().optional(),
  issuingAuthority: z.string().optional(),
  validFrom: z.string().optional(),
  renewedOn: z.string().optional(),
  cost: optionalNumberString,
  expiresOn: z.string().optional(),
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  kind: "Registration",
  registrationNumber: "",
  issuingAuthority: "",
  validFrom: "",
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

  const { data: households } = useHouseholds();
  const household = households?.find((item) => item.id === householdId);
  const { data: assetTypeRegistry } = useAssetTypeRegistry();
  const { data: regionRegistry } = useRegionRegistry();

  const { data: asset } = useQuery({
    queryKey: ["households", householdId, "assets", assetId],
    queryFn: () => getAsset(householdId, assetId),
  });
  const definition = findDefinition(assetTypeRegistry, asset?.category);

  const queryKey = ["households", householdId, "assets", assetId, "registrations"];
  const { data: registrations } = useQuery({
    queryKey,
    queryFn: () => listRegistrations(householdId, assetId),
  });
  const list = registrations ?? [];

  const nudges = computePermitNudges(definition, list);
  const authoritySuggestions = issuingAuthoritySuggestions(
    regionRegistry,
    household?.country,
    household?.region
  );

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
  }

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) => {
      const payload = {
        kind: values.kind,
        registrationNumber: parseOptionalText(values.registrationNumber),
        issuingAuthority: parseOptionalText(values.issuingAuthority),
        validFrom: parseOptionalText(values.validFrom),
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

  function openCreate(kind?: RegistrationKind) {
    setEditing(null);
    form.reset({ ...defaultValues, kind: kind ?? "Registration" });
    setOpen(true);
  }

  function openEdit(registration: RegistrationResponse) {
    setEditing(registration);
    form.reset({
      kind: registration.kind,
      registrationNumber: textToInputValue(registration.registrationNumber),
      issuingAuthority: textToInputValue(registration.issuingAuthority),
      validFrom: textToInputValue(registration.validFrom),
      renewedOn: textToInputValue(registration.renewedOn),
      cost: numberToInputValue(registration.cost),
      expiresOn: textToInputValue(registration.expiresOn),
      notes: textToInputValue(registration.notes),
    });
    setOpen(true);
  }

  function openRenew(registration: RegistrationResponse) {
    setEditing(null);
    form.reset({
      kind: registration.kind,
      registrationNumber: textToInputValue(registration.registrationNumber),
      issuingAuthority: textToInputValue(registration.issuingAuthority),
      validFrom: "",
      renewedOn: "",
      cost: "",
      expiresOn: "",
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

  const selectedKind = form.watch("kind");

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
              <Button size="sm" onClick={() => openCreate()}>
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
                    name="kind"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Kind</FormLabel>
                        <Select value={field.value} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {KINDS.map((kind) => (
                              <SelectItem key={kind} value={kind}>
                                {KIND_LABELS[kind]}
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
                    name="registrationNumber"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{NUMBER_LABELS[selectedKind]}</FormLabel>
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
                          <IssuingAuthorityCombobox
                            value={field.value ?? ""}
                            onChange={field.onChange}
                            onBlur={field.onBlur}
                            suggestions={authoritySuggestions}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="validFrom"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Valid from</FormLabel>
                        <FormControl>
                          <Input type="date" {...field} />
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
                          <MarkdownEditor value={field.value} onChange={field.onChange} onBlur={field.onBlur} />
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

      {nudges.length > 0 && (
        <div className="space-y-2">
          {nudges.map((kind) => (
            <p
              key={kind}
              className="rounded-md bg-warning-bg px-3 py-2 text-sm text-warning"
            >
              {permitNudgeMessage(definition?.displayLabel ?? "This asset", kind)}
            </p>
          ))}
        </div>
      )}

      {list.length === 0 ? (
        <p className="text-sm text-muted-foreground">No registrations yet. Log the first renewal.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Kind</TableHead>
              <TableHead>Number</TableHead>
              <TableHead>Issuing authority</TableHead>
              <TableHead>Valid from</TableHead>
              <TableHead>Renewed on</TableHead>
              <TableHead>Cost</TableHead>
              <TableHead>Expires on</TableHead>
              <TableHead>Document</TableHead>
              {canEdit && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {list.map((registration) => (
              <TableRow key={registration.id}>
                <TableCell>
                  <RegistrationKindBadge kind={registration.kind} />
                </TableCell>
                <TableCell>{registration.registrationNumber ?? "—"}</TableCell>
                <TableCell>{registration.issuingAuthority ?? "—"}</TableCell>
                <TableCell>{registration.validFrom ?? "—"}</TableCell>
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
                    <Button size="sm" variant="outline" onClick={() => openRenew(registration)}>
                      Renew
                    </Button>
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
