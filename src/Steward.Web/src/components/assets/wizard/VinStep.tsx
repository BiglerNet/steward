import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { isValidVinFormat } from "@/lib/vinDecodeMapping";

interface VinStepProps {
  vin: string;
  onVinChange: (vin: string) => void;
  decodePending: boolean;
  bestEffort: boolean;
  onBack: () => void;
  onSkip: () => void;
  onContinue: () => void;
}

export function VinStep({ vin, onVinChange, decodePending, bestEffort, onBack, onSkip, onContinue }: VinStepProps) {
  const [showValidationError, setShowValidationError] = useState(false);

  function handleContinueClick() {
    if (vin.length === 0) {
      onSkip();
      return;
    }
    if (!isValidVinFormat(vin)) {
      setShowValidationError(true);
      return;
    }
    setShowValidationError(false);
    onContinue();
  }

  return (
    <div className="space-y-4">
      <p className="text-small text-muted-foreground">
        Enter the VIN and we'll prefill year, make, model, and engine specs — you can change anything.
      </p>

      {bestEffort && (
        <p className="rounded-md border border-border bg-muted px-3 py-2 text-small text-muted-foreground">
          Decoded data may be sparse for this asset type.
        </p>
      )}

      <div className="space-y-2">
        <Label htmlFor="wizard-vin">VIN</Label>
        <Input
          id="wizard-vin"
          value={vin}
          onChange={(event) => {
            onVinChange(event.target.value.toUpperCase());
            setShowValidationError(false);
          }}
          maxLength={17}
          placeholder="17-character VIN"
          disabled={decodePending}
        />
        {showValidationError && (
          <p className="text-small text-destructive">Enter a valid 17-character VIN, or use Skip to continue without one.</p>
        )}
      </div>

      <div className="flex justify-between">
        <Button type="button" variant="outline" onClick={onBack} disabled={decodePending}>
          Back
        </Button>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={onSkip} disabled={decodePending}>
            Skip
          </Button>
          <Button type="button" onClick={handleContinueClick} disabled={decodePending}>
            {decodePending ? "Decoding…" : "Continue"}
          </Button>
        </div>
      </div>
    </div>
  );
}
