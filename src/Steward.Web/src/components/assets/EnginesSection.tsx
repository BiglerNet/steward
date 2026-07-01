import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useParams } from "react-router";
import { toast } from "sonner";
import { z } from "zod";
import { createEngine, deleteEngine, updateEngine } from "@/api/engines";
import type { EngineResponse, EngineType, FuelType } from "@/api/types";
import { ftLbsToNm, formatTorque, formatVolume, litresToQt, nmToFtLbs, qtToLitres } from "@/lib/units";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useEngines } from "@/hooks/useEngines";
import { numberToInputValue, optionalNumberString, parseOptionalNumber, parseOptionalText, textToInputValue } from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";

const ENGINE_TYPES: EngineType[] = ["Ice", "Electric", "Hybrid"];
const FUEL_TYPES: FuelType[] = ["Gasoline", "Diesel", "TwoStroke", "FourStroke", "Electric", "None"];

const OCTANE_VALUES = ["87", "89", "91", "93"] as const;

const schema = z.object({
  label: z.string().min(1, "Label is required"),
  make: z.string().optional(),
  model: z.string().optional(),
  serialNumber: z.string().optional(),
  year: optionalNumberString,
  engineType: z.enum(["Ice", "Electric", "Hybrid"]),
  fuelType: z.enum(["Gasoline", "Diesel", "TwoStroke", "FourStroke", "Electric", "None"]),
  cylinders: optionalNumberString,
  displacementCc: optionalNumberString,
  installedDate: z.string().optional(),
  installedAtAssetMiles: optionalNumberString,
  installedAtAssetHours: optionalNumberString,
  horsepowerHp: optionalNumberString,
  torqueFtLbs: optionalNumberString,
  oilCapacityQt: optionalNumberString,
  recommendedOilType: z.string().optional(),
  coolantCapacityQt: optionalNumberString,
  recommendedOctane: z.enum(["", "87", "89", "91", "93"]).optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  label: "",
  make: "",
  model: "",
  serialNumber: "",
  year: "",
  engineType: "Ice",
  fuelType: "Gasoline",
  cylinders: "",
  displacementCc: "",
  installedDate: "",
  installedAtAssetMiles: "",
  installedAtAssetHours: "",
  horsepowerHp: "",
  torqueFtLbs: "",
  oilCapacityQt: "",
  recommendedOilType: "",
  coolantCapacityQt: "",
  recommendedOctane: "",
};

export function EnginesSection() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit, canDeleteStructural } = useHouseholdRole();
  const queryClient = useQueryClient();
  const { data: engines } = useEngines(householdId, assetId);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<EngineResponse | null>(null);

  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets", assetId, "engines"] });
  }

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) => {
      const torqueFtLbs = parseOptionalNumber(values.torqueFtLbs);
      const oilQt = parseOptionalNumber(values.oilCapacityQt);
      const coolantQt = parseOptionalNumber(values.coolantCapacityQt);
      const octane = values.recommendedOctane ? parseInt(values.recommendedOctane) : null;
      const payload = {
        label: values.label,
        make: parseOptionalText(values.make),
        model: parseOptionalText(values.model),
        serialNumber: parseOptionalText(values.serialNumber),
        year: parseOptionalNumber(values.year),
        engineType: values.engineType,
        fuelType: values.fuelType,
        cylinders: parseOptionalNumber(values.cylinders),
        displacementCc: parseOptionalNumber(values.displacementCc),
        installedDate: parseOptionalText(values.installedDate),
        installedAtAssetMiles: parseOptionalNumber(values.installedAtAssetMiles),
        installedAtAssetHours: parseOptionalNumber(values.installedAtAssetHours),
        horsepowerHp: parseOptionalNumber(values.horsepowerHp),
        torqueNm: torqueFtLbs != null ? parseFloat(ftLbsToNm(torqueFtLbs).toFixed(2)) : null,
        oilCapacityL: oilQt != null ? parseFloat(qtToLitres(oilQt).toFixed(3)) : null,
        recommendedOilType: parseOptionalText(values.recommendedOilType),
        coolantCapacityL: coolantQt != null ? parseFloat(qtToLitres(coolantQt).toFixed(3)) : null,
        recommendedOctane: octane,
      };
      return editing
        ? updateEngine(householdId, assetId, editing.id, payload)
        : createEngine(householdId, assetId, payload);
    },
    onSuccess: () => {
      invalidate();
      setOpen(false);
      setEditing(null);
      form.reset(defaultValues);
      toast.success(editing ? "Engine updated." : "Engine added.");
    },
    onError: () => toast.error("Couldn't save this engine. Please try again."),
  });

  const deleteMutation = useMutation({
    mutationFn: (engine: EngineResponse) => deleteEngine(householdId, assetId, engine.id),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't delete this engine."),
  });

  function openCreate() {
    setEditing(null);
    form.reset(defaultValues);
    setOpen(true);
  }

  function openEdit(engine: EngineResponse) {
    setEditing(engine);
    form.reset({
      label: engine.label,
      make: textToInputValue(engine.make),
      model: textToInputValue(engine.model),
      serialNumber: textToInputValue(engine.serialNumber),
      year: numberToInputValue(engine.year),
      engineType: engine.engineType,
      fuelType: engine.fuelType,
      cylinders: numberToInputValue(engine.cylinders),
      displacementCc: numberToInputValue(engine.displacementCc),
      installedDate: textToInputValue(engine.installedDate),
      installedAtAssetMiles: numberToInputValue(engine.installedAtAssetMiles),
      installedAtAssetHours: numberToInputValue(engine.installedAtAssetHours),
      horsepowerHp: numberToInputValue(engine.horsepowerHp),
      torqueFtLbs: engine.torqueNm != null ? String(Math.round(nmToFtLbs(engine.torqueNm))) : "",
      oilCapacityQt: engine.oilCapacityL != null ? String(litresToQt(engine.oilCapacityL).toFixed(2)) : "",
      recommendedOilType: textToInputValue(engine.recommendedOilType),
      coolantCapacityQt: engine.coolantCapacityL != null ? String(litresToQt(engine.coolantCapacityL).toFixed(2)) : "",
      recommendedOctane: engine.recommendedOctane != null ? String(engine.recommendedOctane) as "" | "87" | "89" | "91" | "93" : "",
    });
    setOpen(true);
  }

  function handleDelete(engine: EngineResponse) {
    if (window.confirm(`Delete engine "${engine.label}"? This can't be undone.`)) {
      deleteMutation.mutate(engine);
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">Engines</h2>
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
                Add engine
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{editing ? "Edit engine" : "Add engine"}</DialogTitle>
              </DialogHeader>
              <Form {...form}>
                <form
                  onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}
                  className="space-y-4"
                >
                  <FormField
                    control={form.control}
                    name="label"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Label</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="make"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Make</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="model"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Model</FormLabel>
                        <FormControl>
                          <Input {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="serialNumber"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Serial number</FormLabel>
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
                    name="engineType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Engine type</FormLabel>
                        <Select value={field.value} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {ENGINE_TYPES.map((type) => (
                              <SelectItem key={type} value={type}>
                                {type}
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
                    name="fuelType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Fuel type</FormLabel>
                        <Select value={field.value} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {FUEL_TYPES.map((type) => (
                              <SelectItem key={type} value={type}>
                                {type}
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
                    name="cylinders"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Cylinders</FormLabel>
                        <FormControl>
                          <Input type="number" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="displacementCc"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Displacement (cc)</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.1" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="installedDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Installed date</FormLabel>
                        <FormControl>
                          <Input type="date" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="installedAtAssetMiles"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Installed at asset miles</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.1" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="installedAtAssetHours"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Installed at asset hours</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.1" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="horsepowerHp"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>HP</FormLabel>
                        <FormControl>
                          <Input type="number" step="1" placeholder="e.g. 355" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="torqueFtLbs"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Torque (ft-lbs)</FormLabel>
                        <FormControl>
                          <Input type="number" step="1" placeholder="e.g. 350" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="oilCapacityQt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Oil capacity (qt)</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.1" placeholder="e.g. 5" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="recommendedOilType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Recommended oil type</FormLabel>
                        <FormControl>
                          <Input placeholder="e.g. 5W-30 Full Synthetic" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="coolantCapacityQt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Coolant capacity (qt)</FormLabel>
                        <FormControl>
                          <Input type="number" step="0.1" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="recommendedOctane"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Recommended octane</FormLabel>
                        <Select value={field.value ?? ""} onValueChange={field.onChange}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder="Select octane" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="">—</SelectItem>
                            {OCTANE_VALUES.map((v) => (
                              <SelectItem key={v} value={v}>{v}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
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

      {!engines || engines.length === 0 ? (
        <p className="text-sm text-muted-foreground">No engines yet.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Label</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>HP</TableHead>
              <TableHead>Torque</TableHead>
              <TableHead>Oil</TableHead>
              {(canEdit || canDeleteStructural) && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {engines.map((engine) => (
              <TableRow key={engine.id}>
                <TableCell>{engine.label}</TableCell>
                <TableCell>{engine.engineType}</TableCell>
                <TableCell>{engine.status}</TableCell>
                <TableCell>{engine.horsepowerHp ?? "—"}</TableCell>
                <TableCell>{engine.torqueNm != null ? formatTorque(engine.torqueNm) : "—"}</TableCell>
                <TableCell>{engine.oilCapacityL != null ? formatVolume(engine.oilCapacityL) : "—"}</TableCell>
                {(canEdit || canDeleteStructural) && (
                  <TableCell className="text-right space-x-2">
                    {canEdit && (
                      <Button size="sm" variant="outline" onClick={() => openEdit(engine)}>
                        Edit
                      </Button>
                    )}
                    {canDeleteStructural && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={deleteMutation.isPending}
                        onClick={() => handleDelete(engine)}
                      >
                        Delete
                      </Button>
                    )}
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
