import { useState } from "react";
import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { parseOptionalNumber } from "@/lib/formHelpers";
import type { CreateTemplateStepRequest, SuggestedPartDto, TemplateStepResponse } from "@/api/types";

interface TemplateStepFormDialogProps {
  initial?: TemplateStepResponse;
  onSave: (request: CreateTemplateStepRequest) => void;
  trigger: React.ReactNode;
}

export function TemplateStepFormDialog({ initial, onSave, trigger }: TemplateStepFormDialogProps) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState(initial?.text ?? "");
  const [engineScoped, setEngineScoped] = useState(initial?.engineScoped ?? false);
  const [months, setMonths] = useState(initial?.recurrenceIntervalMonths?.toString() ?? "");
  const [miles, setMiles] = useState(initial?.recurrenceIntervalMiles?.toString() ?? "");
  const [hours, setHours] = useState(initial?.recurrenceIntervalHours?.toString() ?? "");
  const [suggestedParts, setSuggestedParts] = useState<SuggestedPartDto[]>(
    initial?.suggestedParts ?? []
  );

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!text.trim()) return;

    onSave({
      text: text.trim(),
      engineScoped,
      recurrenceIntervalMonths: parseOptionalNumber(months),
      recurrenceIntervalMiles: parseOptionalNumber(miles),
      recurrenceIntervalHours: parseOptionalNumber(hours),
      suggestedParts: suggestedParts.filter((p) => p.name.trim().length > 0),
    });
    setOpen(false);
  }

  function updatePart(index: number, patch: Partial<SuggestedPartDto>) {
    setSuggestedParts((current) => current.map((p, i) => (i === index ? { ...p, ...patch } : p)));
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{initial ? "Edit step" : "Add step"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="space-y-2">
            <Label htmlFor="step-text">Step</Label>
            <Input id="step-text" value={text} onChange={(e) => setText(e.target.value)} autoFocus />
          </div>

          <div className="flex items-center gap-2">
            <Checkbox
              id="step-engine-scoped"
              checked={engineScoped}
              onChange={(e) => setEngineScoped(e.target.checked)}
            />
            <Label htmlFor="step-engine-scoped">Applies per engine</Label>
          </div>

          <div className="grid grid-cols-3 gap-2">
            <div className="space-y-2">
              <Label htmlFor="step-months">Every (months)</Label>
              <Input id="step-months" type="number" value={months} onChange={(e) => setMonths(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="step-miles">Every (miles)</Label>
              <Input id="step-miles" type="number" value={miles} onChange={(e) => setMiles(e.target.value)} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="step-hours">Every (hours)</Label>
              <Input id="step-hours" type="number" value={hours} onChange={(e) => setHours(e.target.value)} />
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Suggested parts</Label>
              <Button
                type="button"
                size="sm"
                variant="outline"
                onClick={() => setSuggestedParts((current) => [...current, { name: "", quantity: 1 }])}
              >
                <Plus className="h-3.5 w-3.5" /> Add part
              </Button>
            </div>
            {suggestedParts.map((part, index) => (
              <div key={index} className="flex items-center gap-2">
                <Input
                  placeholder="Part name"
                  value={part.name}
                  onChange={(e) => updatePart(index, { name: e.target.value })}
                />
                <Input
                  className="w-20"
                  type="number"
                  step="0.01"
                  value={part.quantity}
                  onChange={(e) => updatePart(index, { quantity: Number(e.target.value) || 0 })}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Remove suggested part"
                  onClick={() => setSuggestedParts((current) => current.filter((_, i) => i !== index))}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>

          <DialogFooter>
            <Button type="submit" disabled={!text.trim()}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
