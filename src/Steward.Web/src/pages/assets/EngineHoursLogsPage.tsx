import { useState } from "react";
import { useParams } from "react-router";
import { z } from "zod";
import {
  createEngineHoursLog,
  deleteEngineHoursLog,
  listEngineHoursLogs,
  updateEngineHoursLog,
} from "@/api/tracking";
import type { EngineHoursLogResponse } from "@/api/types";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";
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
  hoursReading: optionalNumberString,
  tripHours: optionalNumberString,
  notes: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const defaultValues: FormValues = { date: "", hoursReading: "", tripHours: "", notes: "" };

export function EngineHoursLogsPage() {
  const { householdId, assetId } = useParams() as { householdId: string; assetId: string };
  const { canEdit } = useHouseholdRole();
  const { data: engines } = useEngines(householdId, assetId);
  const [selectedEngineId, setSelectedEngineId] = useState<string>("");
  const engineId = selectedEngineId || engines?.[0]?.id || "";

  if (engines && engines.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Add an engine to this asset before logging engine hours.
      </p>
    );
  }

  return (
    <div className="space-y-4">
      <div className="max-w-xs">
        <label className="text-sm font-medium">Engine</label>
        <Select value={engineId} onValueChange={setSelectedEngineId}>
          <SelectTrigger>
            <SelectValue placeholder="Select an engine" />
          </SelectTrigger>
          <SelectContent>
            {engines?.map((engine) => (
              <SelectItem key={engine.id} value={engine.id}>
                {engine.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {engineId && (
        <TrackingLogSection<EngineHoursLogResponse, FormValues>
          title="Engine Hours Logs"
          emptyMessage="No engine hours logs yet. Add the first entry."
          queryKey={["households", householdId, "assets", assetId, "engines", engineId, "hours-logs"]}
          canEdit={canEdit}
          columns={[
            { key: "date", header: "Date", render: (r) => r.date },
            { key: "hoursReading", header: "Hours reading", render: (r) => r.hoursReading ?? "—" },
            { key: "tripHours", header: "Trip hours", render: (r) => r.tripHours ?? "—" },
          ]}
          list={() => listEngineHoursLogs(householdId, assetId, engineId)}
          create={(values) =>
            createEngineHoursLog(householdId, assetId, engineId, {
              date: values.date,
              hoursReading: parseOptionalNumber(values.hoursReading),
              tripHours: parseOptionalNumber(values.tripHours),
              notes: parseOptionalText(values.notes),
            })
          }
          update={(id, values) =>
            updateEngineHoursLog(householdId, assetId, engineId, id, {
              date: values.date,
              hoursReading: parseOptionalNumber(values.hoursReading),
              tripHours: parseOptionalNumber(values.tripHours),
              notes: parseOptionalText(values.notes),
            })
          }
          remove={(id) => deleteEngineHoursLog(householdId, assetId, engineId, id)}
          getId={(r) => r.id}
          sortValue={(r) => r.date}
          schema={schema}
          defaultValues={defaultValues}
          toFormValues={(r) => ({
            date: r.date,
            hoursReading: numberToInputValue(r.hoursReading),
            tripHours: numberToInputValue(r.tripHours),
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
                name="hoursReading"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Hours reading</FormLabel>
                    <FormControl>
                      <Input type="number" step="0.1" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="tripHours"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Trip hours</FormLabel>
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
                      <MarkdownEditor value={field.value} onChange={field.onChange} onBlur={field.onBlur} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </>
          )}
        />
      )}
    </div>
  );
}
