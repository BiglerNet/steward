import { useParams } from "react-router";
import { z } from "zod";
import { createMileageLog, deleteMileageLog, listMileageLogs, updateMileageLog } from "@/api/tracking";
import type { MileageLogResponse } from "@/api/types";
import { TrackingLogSection } from "@/components/tracking/TrackingLogSection";
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
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
  odometerReading: optionalNumberString,
  tripMiles: optionalNumberString,
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = { date: "", odometerReading: "", tripMiles: "", notes: "" };

export function MileageLogsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();

  return (
    <TrackingLogSection<MileageLogResponse, FormValues>
      title="Mileage Logs"
      emptyMessage="No mileage logs yet. Add the first entry."
      queryKey={["households", householdId, "assets", assetId, "mileage-logs"]}
      canEdit={canEdit}
      columns={[
        { key: "date", header: "Date", render: (r) => r.date },
        { key: "odometerReading", header: "Odometer", render: (r) => r.odometerReading ?? "—" },
        { key: "tripMiles", header: "Trip miles", render: (r) => r.tripMiles ?? "—" },
      ]}
      list={() => listMileageLogs(householdId, assetId)}
      create={(values) =>
        createMileageLog(householdId, assetId, {
          date: values.date,
          odometerReading: parseOptionalNumber(values.odometerReading),
          tripMiles: parseOptionalNumber(values.tripMiles),
          notes: parseOptionalText(values.notes),
        })
      }
      update={(id, values) =>
        updateMileageLog(householdId, assetId, id, {
          date: values.date,
          odometerReading: parseOptionalNumber(values.odometerReading),
          tripMiles: parseOptionalNumber(values.tripMiles),
          notes: parseOptionalText(values.notes),
        })
      }
      remove={(id) => deleteMileageLog(householdId, assetId, id)}
      getId={(r) => r.id}
      sortValue={(r) => r.date}
      schema={schema}
      defaultValues={defaultValues}
      toFormValues={(r) => ({
        date: r.date,
        odometerReading: numberToInputValue(r.odometerReading),
        tripMiles: numberToInputValue(r.tripMiles),
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
            name="odometerReading"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Odometer reading</FormLabel>
                <FormControl>
                  <Input type="number" step="0.1" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="tripMiles"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Trip miles</FormLabel>
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
