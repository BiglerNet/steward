import { useMemo } from "react";
import { useParams } from "react-router";
import { z } from "zod";
import { createFuelLog, deleteFuelLog, listFuelLogs, updateFuelLog } from "@/api/tracking";
import type { EngineResponse, FuelLogResponse, FuelLogType, VolumeUnit } from "@/api/types";
import { TrackingLogSection } from "@/components/tracking/TrackingLogSection";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useEngines } from "@/hooks/useEngines";
import {
  numberToInputValue,
  optionalNumberString,
  parseOptionalNumber,
  parseOptionalText,
  textToInputValue,
} from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";

const LOG_TYPES: FuelLogType[] = ["Fillup", "Consumption"];
const ALL_UNITS: VolumeUnit[] = ["Gallons", "Liters", "Kwh"];

function unitsForEngine(engine: EngineResponse): VolumeUnit[] {
  return engine.engineType === "Electric" ? ["Kwh"] : ["Gallons", "Liters"];
}

function loggableEnginesOf(engines: EngineResponse[] | undefined): EngineResponse[] {
  if (!engines) return [];
  return engines.filter(
    (e) =>
      e.status === "Active" &&
      (e.engineType === "Ice" || (e.engineType === "Electric" && e.isExternallyChargeable === true))
  );
}

function buildSchema(loggableEngines: EngineResponse[]) {
  return z
    .object({
      logType: z.enum(["Fillup", "Consumption"]),
      date: z.string().min(1, "Date is required"),
      quantity: z.string().min(1, "Quantity is required"),
      unit: z.enum(["Gallons", "Liters", "Kwh"]),
      engineId: z.string().optional(),
      fuelGrade: z.string().optional(),
      pricePerUnit: optionalNumberString,
      totalCost: optionalNumberString,
      milesAtLog: optionalNumberString,
      hoursAtLog: optionalNumberString,
      notes: z.string().optional(),
    })
    .superRefine((values, ctx) => {
      if (loggableEngines.length >= 2 && !values.engineId) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["engineId"], message: "Select an engine." });
      }
    });
}

type FormValues = z.infer<ReturnType<typeof buildSchema>>;

export function FuelLogsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();
  const { data: engines } = useEngines(householdId, assetId);

  const loggableEngines = useMemo(() => loggableEnginesOf(engines), [engines]);
  const schema = useMemo(() => buildSchema(loggableEngines), [loggableEngines]);

  const defaultUnit: VolumeUnit =
    loggableEngines.length === 1 ? unitsForEngine(loggableEngines[0])[0] : "Gallons";

  const defaultValues: FormValues = {
    logType: "Fillup",
    date: "",
    quantity: "",
    unit: defaultUnit,
    engineId: loggableEngines.length === 1 ? loggableEngines[0].id : "",
    fuelGrade: "",
    pricePerUnit: "",
    totalCost: "",
    milesAtLog: "",
    hoursAtLog: "",
    notes: "",
  };

  function resolveEngineId(values: FormValues): string | null {
    if (loggableEngines.length === 1) return loggableEngines[0].id;
    if (loggableEngines.length >= 2) return values.engineId ?? null;
    return null;
  }

  return (
    <TrackingLogSection<FuelLogResponse, FormValues>
      title="Fuel Logs"
      emptyMessage="No fuel logs yet. Add the first entry."
      queryKey={["households", householdId, "assets", assetId, "fuel-logs"]}
      canEdit={canEdit}
      columns={[
        { key: "date", header: "Date", render: (r) => r.date },
        { key: "logType", header: "Type", render: (r) => r.logType },
        { key: "quantity", header: "Quantity", render: (r) => `${r.quantity} ${r.unit}` },
        { key: "totalCost", header: "Total cost", render: (r) => r.totalCost ?? "—" },
      ]}
      list={() => listFuelLogs(householdId, assetId)}
      create={(values) =>
        createFuelLog(householdId, assetId, {
          logType: values.logType,
          date: values.date,
          quantity: Number(values.quantity),
          unit: values.unit,
          fuelGrade: parseOptionalText(values.fuelGrade),
          pricePerUnit: parseOptionalNumber(values.pricePerUnit),
          totalCost: parseOptionalNumber(values.totalCost),
          milesAtLog: parseOptionalNumber(values.milesAtLog),
          hoursAtLog: parseOptionalNumber(values.hoursAtLog),
          engineId: resolveEngineId(values),
          notes: parseOptionalText(values.notes),
        })
      }
      update={(id, values) =>
        updateFuelLog(householdId, assetId, id, {
          logType: values.logType,
          date: values.date,
          quantity: Number(values.quantity),
          unit: values.unit,
          fuelGrade: parseOptionalText(values.fuelGrade),
          pricePerUnit: parseOptionalNumber(values.pricePerUnit),
          totalCost: parseOptionalNumber(values.totalCost),
          milesAtLog: parseOptionalNumber(values.milesAtLog),
          hoursAtLog: parseOptionalNumber(values.hoursAtLog),
          engineId: resolveEngineId(values),
          notes: parseOptionalText(values.notes),
        })
      }
      remove={(id) => deleteFuelLog(householdId, assetId, id)}
      getId={(r) => r.id}
      sortValue={(r) => r.date}
      schema={schema}
      defaultValues={defaultValues}
      toFormValues={(r) => ({
        logType: r.logType,
        date: r.date,
        quantity: numberToInputValue(r.quantity),
        unit: r.unit,
        engineId: r.engineId ?? "",
        fuelGrade: textToInputValue(r.fuelGrade),
        pricePerUnit: numberToInputValue(r.pricePerUnit),
        totalCost: numberToInputValue(r.totalCost),
        milesAtLog: numberToInputValue(r.milesAtLog),
        hoursAtLog: numberToInputValue(r.hoursAtLog),
        notes: textToInputValue(r.notes),
      })}
      renderFields={(form) => {
        const selectedEngineId = form.watch("engineId");
        const selectedEngine = loggableEngines.find((e) => e.id === selectedEngineId);
        const availableUnits =
          loggableEngines.length >= 2
            ? selectedEngine
              ? unitsForEngine(selectedEngine)
              : ALL_UNITS
            : loggableEngines.length === 1
              ? unitsForEngine(loggableEngines[0])
              : ALL_UNITS;

        return (
          <>
            <FormField
              control={form.control}
              name="logType"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Log type</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {LOG_TYPES.map((type) => (
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
              name="date"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Date</FormLabel>
                  <FormControl>
                    <Input type="date" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            {loggableEngines.length >= 2 && (
              <FormField
                control={form.control}
                name="engineId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Engine</FormLabel>
                    <Select
                      value={field.value ?? ""}
                      onValueChange={(value) => {
                        field.onChange(value);
                        const engine = loggableEngines.find((e) => e.id === value);
                        if (engine) {
                          form.setValue("unit", unitsForEngine(engine)[0]);
                        }
                      }}
                    >
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select engine" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {loggableEngines.map((engine) => (
                          <SelectItem key={engine.id} value={engine.id}>
                            {engine.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />
            )}
            <FormField
              control={form.control}
              name="quantity"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Quantity</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.01" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="unit"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Unit</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {availableUnits.map((unit) => (
                        <SelectItem key={unit} value={unit}>
                          {unit}
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
              name="fuelGrade"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Fuel grade</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="pricePerUnit"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Price per unit</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.001" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="totalCost"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Total cost</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.01" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="milesAtLog"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Miles at log</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.1" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="hoursAtLog"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Hours at log</FormLabel>
                  <FormControl>
                    <Input type="number" step="0.1" {...field} />
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
          </>
        );
      }}
    />
  );
}
