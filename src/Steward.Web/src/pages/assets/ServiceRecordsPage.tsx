import { useParams } from "react-router";
import { z } from "zod";
import {
  createServiceRecord,
  deleteServiceRecord,
  listServiceRecords,
  updateServiceRecord,
} from "@/api/tracking";
import type { ServiceRecordResponse } from "@/api/types";
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

const schema = z.object({
  date: z.string().min(1, "Date is required"),
  description: z.string().min(1, "Description is required"),
  providerName: z.string().optional(),
  cost: optionalNumberString,
  odometerMiles: optionalNumberString,
  engineHours: optionalNumberString,
  engineId: z.string().optional(),
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = {
  date: "",
  description: "",
  providerName: "",
  cost: "",
  odometerMiles: "",
  engineHours: "",
  engineId: "",
  notes: "",
};

export function ServiceRecordsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();
  const { data: engines } = useEngines(householdId, assetId);

  return (
    <TrackingLogSection<ServiceRecordResponse, FormValues>
      title="Service Records"
      emptyMessage="No service records yet. Log the first service entry."
      queryKey={["households", householdId, "assets", assetId, "service-records"]}
      canEdit={canEdit}
      columns={[
        { key: "date", header: "Date", render: (r) => r.date },
        { key: "description", header: "Description", render: (r) => r.description },
        { key: "providerName", header: "Provider", render: (r) => r.providerName ?? "—" },
        { key: "cost", header: "Cost", render: (r) => r.cost ?? "—" },
      ]}
      list={() => listServiceRecords(householdId, assetId)}
      create={(values) =>
        createServiceRecord(householdId, assetId, {
          date: values.date,
          description: values.description,
          providerName: parseOptionalText(values.providerName),
          cost: parseOptionalNumber(values.cost),
          odometerMiles: parseOptionalNumber(values.odometerMiles),
          engineHours: parseOptionalNumber(values.engineHours),
          engineId: parseOptionalText(values.engineId),
          notes: parseOptionalText(values.notes),
        })
      }
      update={(id, values) =>
        updateServiceRecord(householdId, assetId, id, {
          date: values.date,
          description: values.description,
          providerName: parseOptionalText(values.providerName),
          cost: parseOptionalNumber(values.cost),
          odometerMiles: parseOptionalNumber(values.odometerMiles),
          engineHours: parseOptionalNumber(values.engineHours),
          engineId: parseOptionalText(values.engineId),
          notes: parseOptionalText(values.notes),
        })
      }
      remove={(id) => deleteServiceRecord(householdId, assetId, id)}
      getId={(r) => r.id}
      sortValue={(r) => r.date}
      schema={schema}
      defaultValues={defaultValues}
      toFormValues={(r) => ({
        date: r.date,
        description: r.description,
        providerName: textToInputValue(r.providerName),
        cost: numberToInputValue(r.cost),
        odometerMiles: numberToInputValue(r.odometerMiles),
        engineHours: numberToInputValue(r.engineHours),
        engineId: textToInputValue(r.engineId),
        notes: textToInputValue(r.notes),
      })}
      renderFields={(form) => (
        <>
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
            name="description"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Description</FormLabel>
                <FormControl>
                  <Input {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="providerName"
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
            name="odometerMiles"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Odometer (miles)</FormLabel>
                <FormControl>
                  <Input type="number" step="0.1" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="engineHours"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Engine hours</FormLabel>
                <FormControl>
                  <Input type="number" step="0.1" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="engineId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Engine (optional)</FormLabel>
                <Select
                  value={field.value || "none"}
                  onValueChange={(value) => field.onChange(value === "none" ? "" : value)}
                >
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="No engine" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="none">No engine</SelectItem>
                    {engines?.map((engine) => (
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
