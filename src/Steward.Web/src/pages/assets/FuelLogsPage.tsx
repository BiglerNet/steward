import { useParams } from "react-router";
import { z } from "zod";
import { createFuelLog, deleteFuelLog, listFuelLogs, updateFuelLog } from "@/api/tracking";
import type { FuelLogResponse, FuelLogType, VolumeUnit } from "@/api/types";
import { TrackingLogSection } from "@/components/tracking/TrackingLogSection";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
  numberToInputValue,
  optionalNumberString,
  parseOptionalNumber,
  parseOptionalText,
  textToInputValue,
} from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";

const LOG_TYPES: FuelLogType[] = ["Fillup", "Consumption"];
const VOLUME_UNITS: VolumeUnit[] = ["Gallons", "Liters"];

const schema = z.object({
  logType: z.enum(["Fillup", "Consumption"]),
  date: z.string().min(1, "Date is required"),
  volume: z.string().min(1, "Volume is required"),
  volumeUnit: z.enum(["Gallons", "Liters"]),
  fuelGrade: z.string().optional(),
  pricePerUnit: optionalNumberString,
  totalCost: optionalNumberString,
  milesAtLog: optionalNumberString,
  hoursAtLog: optionalNumberString,
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  logType: "Fillup",
  date: "",
  volume: "",
  volumeUnit: "Gallons",
  fuelGrade: "",
  pricePerUnit: "",
  totalCost: "",
  milesAtLog: "",
  hoursAtLog: "",
  notes: "",
};

export function FuelLogsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();

  return (
    <TrackingLogSection<FuelLogResponse, FormValues>
      title="Fuel Logs"
      emptyMessage="No fuel logs yet. Add the first entry."
      queryKey={["households", householdId, "assets", assetId, "fuel-logs"]}
      canEdit={canEdit}
      columns={[
        { key: "date", header: "Date", render: (r) => r.date },
        { key: "logType", header: "Type", render: (r) => r.logType },
        { key: "volume", header: "Volume", render: (r) => `${r.volume} ${r.volumeUnit}` },
        { key: "totalCost", header: "Total cost", render: (r) => r.totalCost ?? "—" },
      ]}
      list={() => listFuelLogs(householdId, assetId)}
      create={(values) =>
        createFuelLog(householdId, assetId, {
          logType: values.logType,
          date: values.date,
          volume: Number(values.volume),
          volumeUnit: values.volumeUnit,
          fuelGrade: parseOptionalText(values.fuelGrade),
          pricePerUnit: parseOptionalNumber(values.pricePerUnit),
          totalCost: parseOptionalNumber(values.totalCost),
          milesAtLog: parseOptionalNumber(values.milesAtLog),
          hoursAtLog: parseOptionalNumber(values.hoursAtLog),
          engineId: null,
          notes: parseOptionalText(values.notes),
        })
      }
      update={(id, values) =>
        updateFuelLog(householdId, assetId, id, {
          logType: values.logType,
          date: values.date,
          volume: Number(values.volume),
          volumeUnit: values.volumeUnit,
          fuelGrade: parseOptionalText(values.fuelGrade),
          pricePerUnit: parseOptionalNumber(values.pricePerUnit),
          totalCost: parseOptionalNumber(values.totalCost),
          milesAtLog: parseOptionalNumber(values.milesAtLog),
          hoursAtLog: parseOptionalNumber(values.hoursAtLog),
          engineId: null,
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
        volume: numberToInputValue(r.volume),
        volumeUnit: r.volumeUnit,
        fuelGrade: textToInputValue(r.fuelGrade),
        pricePerUnit: numberToInputValue(r.pricePerUnit),
        totalCost: numberToInputValue(r.totalCost),
        milesAtLog: numberToInputValue(r.milesAtLog),
        hoursAtLog: numberToInputValue(r.hoursAtLog),
        notes: textToInputValue(r.notes),
      })}
      renderFields={(form) => (
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
          <FormField
            control={form.control}
            name="volume"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Volume</FormLabel>
                <FormControl>
                  <Input type="number" step="0.01" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="volumeUnit"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Volume unit</FormLabel>
                <Select value={field.value} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {VOLUME_UNITS.map((unit) => (
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
      )}
    />
  );
}
