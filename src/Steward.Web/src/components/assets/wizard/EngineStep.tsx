import { useEffect } from "react";
import type { UseFormReturn } from "react-hook-form";
import type { FuelType, Mechanism } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { isHybridChoice, WIZARD_ENGINE_TYPES, type EngineFieldsFormValues } from "@/lib/engineFields";

const MECHANISMS: Mechanism[] = ["TwoStroke", "FourStroke", "Diesel", "Rotary"];
const FUEL_TYPES: FuelType[] = ["Gasoline", "Diesel", "Propane"];

interface EngineStepProps {
  form: UseFormReturn<EngineFieldsFormValues>;
  prefilledFromVin: boolean;
  errorMessage: string | null;
  submitting: boolean;
  onBack: () => void;
  onSkip: () => void;
  onSubmit: (values: EngineFieldsFormValues) => void;
  onRetry: () => void;
}

export function EngineStep({
  form,
  prefilledFromVin,
  errorMessage,
  submitting,
  onBack,
  onSkip,
  onSubmit,
  onRetry,
}: EngineStepProps) {
  const wizardEngineType = form.watch("wizardEngineType");
  const isHybrid = isHybridChoice(wizardEngineType);
  const showIceFields = wizardEngineType === "Ice" || isHybrid;

  useEffect(() => {
    if (!isHybrid) {
      form.setValue("electricLabel", "");
      form.setValue("electricHorsepowerHp", "");
      form.setValue("electricTorqueFtLbs", "");
    }
  }, [isHybrid, form]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {prefilledFromVin && (
          <p className="text-small text-muted-foreground">Some fields were prefilled from the VIN decode.</p>
        )}

        <FormField
          control={form.control}
          name="wizardEngineType"
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
                  {WIZARD_ENGINE_TYPES.map((type) => (
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

        {isHybrid && <h3 className="text-h3">Gas engine</h3>}

        <FormField
          control={form.control}
          name="label"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Label</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Main engine" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        {showIceFields && (
          <FormField
            control={form.control}
            name="mechanism"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Mechanism</FormLabel>
                <Select value={field.value ?? ""} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select mechanism" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="">—</SelectItem>
                    {MECHANISMS.map((type) => (
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
        )}
        {showIceFields && (
          <FormField
            control={form.control}
            name="fuelType"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Fuel type</FormLabel>
                <Select value={field.value ?? ""} onValueChange={field.onChange}>
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder="Select fuel type" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="">—</SelectItem>
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
        )}
        {showIceFields && (
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
        )}
        {showIceFields && (
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
        )}
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

        {isHybrid && (
          <>
            <h3 className="text-h3">Electric motor</h3>
            <FormField
              control={form.control}
              name="electricLabel"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Label</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g. Electric motor" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="electricHorsepowerHp"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>HP</FormLabel>
                  <FormControl>
                    <Input type="number" step="1" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="electricTorqueFtLbs"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Torque (ft-lbs)</FormLabel>
                  <FormControl>
                    <Input type="number" step="1" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </>
        )}

        {errorMessage && (
          <div className="space-y-2 rounded-md border border-destructive/50 bg-destructive/5 px-3 py-2">
            <p className="text-small text-destructive">{errorMessage}</p>
            <div className="flex gap-2">
              <Button type="button" size="sm" variant="outline" onClick={onRetry} disabled={submitting}>
                Retry
              </Button>
              <Button type="button" size="sm" variant="outline" onClick={onSkip} disabled={submitting}>
                Skip
              </Button>
            </div>
          </div>
        )}

        <div className="flex justify-between">
          <Button type="button" variant="outline" onClick={onBack} disabled={submitting}>
            Back
          </Button>
          <div className="flex gap-2">
            <Button type="button" variant="outline" onClick={onSkip} disabled={submitting}>
              Skip
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Saving…" : "Continue"}
            </Button>
          </div>
        </div>
      </form>
    </Form>
  );
}
