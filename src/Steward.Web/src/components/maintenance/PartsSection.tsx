import { useState } from "react";
import { Button } from "@/components/ui/button";
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { parseOptionalNumber, parseOptionalText } from "@/lib/formHelpers";
import type { CreatePartLineRequest, PartLineResponse, PartLineStatus } from "@/api/types";

const PART_STATUSES: PartLineStatus[] = ["Needed", "Ordered", "Received"];

interface PartsSectionProps {
  partLines: PartLineResponse[];
  canEdit: boolean;
  onAdd: (request: CreatePartLineRequest) => void;
  onEdit: (partLineId: string, request: CreatePartLineRequest) => void;
  onSetStatus: (partLineId: string, status: PartLineStatus) => void;
  onDelete: (partLineId: string) => void;
}

export function PartsSection({ partLines, canEdit, onAdd, onEdit, onSetStatus, onDelete }: PartsSectionProps) {
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-h3">Parts</h3>
        {canEdit && <PartLineFormDialog onSave={onAdd} trigger={<Button size="sm" variant="outline">Add part</Button>} />}
      </div>

      {partLines.length === 0 ? (
        <p className="text-sm text-muted-foreground">No parts yet.</p>
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Qty</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Cost</TableHead>
              {canEdit && <TableHead className="text-right">Actions</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {partLines.map((part) => (
              <TableRow key={part.id}>
                <TableCell>
                  {part.name}
                  {part.vendor && <span className="ml-1 text-xs text-muted-foreground">({part.vendor})</span>}
                </TableCell>
                <TableCell>{part.quantity}</TableCell>
                <TableCell>
                  {canEdit ? (
                    <Select value={part.status} onValueChange={(v) => onSetStatus(part.id, v as PartLineStatus)}>
                      <SelectTrigger className="h-8 w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {PART_STATUSES.map((status) => (
                          <SelectItem key={status} value={status}>
                            {status}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  ) : (
                    part.status
                  )}
                </TableCell>
                <TableCell>{part.cost ?? "—"}</TableCell>
                {canEdit && (
                  <TableCell className="space-x-2 text-right">
                    <PartLineFormDialog
                      initial={part}
                      onSave={(request) => onEdit(part.id, request)}
                      trigger={
                        <Button size="sm" variant="outline">
                          Edit
                        </Button>
                      }
                    />
                    <Button size="sm" variant="outline" onClick={() => onDelete(part.id)}>
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

interface PartLineFormDialogProps {
  initial?: PartLineResponse;
  onSave: (request: CreatePartLineRequest) => void;
  trigger: React.ReactNode;
}

function PartLineFormDialog({ initial, onSave, trigger }: PartLineFormDialogProps) {
  const [open, setOpen] = useState(false);
  const [name, setName] = useState(initial?.name ?? "");
  const [quantity, setQuantity] = useState(String(initial?.quantity ?? 1));
  const [partNumber, setPartNumber] = useState(initial?.partNumber ?? "");
  const [vendor, setVendor] = useState(initial?.vendor ?? "");
  const [trackingNumber, setTrackingNumber] = useState(initial?.trackingNumber ?? "");
  const [orderUrl, setOrderUrl] = useState(initial?.orderUrl ?? "");
  const [cost, setCost] = useState(initial?.cost != null ? String(initial.cost) : "");

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!name.trim()) return;

    onSave({
      name: name.trim(),
      quantity: parseOptionalNumber(quantity) ?? 1,
      partNumber: parseOptionalText(partNumber),
      vendor: parseOptionalText(vendor),
      trackingNumber: parseOptionalText(trackingNumber),
      orderUrl: parseOptionalText(orderUrl),
      cost: parseOptionalNumber(cost),
    });
    setOpen(false);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{initial ? "Edit part" : "Add part"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="space-y-2">
            <Label htmlFor="part-name">Name</Label>
            <Input id="part-name" value={name} onChange={(e) => setName(e.target.value)} autoFocus />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-quantity">Quantity</Label>
            <Input
              id="part-quantity"
              type="number"
              step="0.01"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-number">Part number</Label>
            <Input id="part-number" value={partNumber} onChange={(e) => setPartNumber(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-vendor">Vendor</Label>
            <Input id="part-vendor" value={vendor} onChange={(e) => setVendor(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-tracking">Tracking number</Label>
            <Input id="part-tracking" value={trackingNumber} onChange={(e) => setTrackingNumber(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-order-url">Order URL</Label>
            <Input id="part-order-url" value={orderUrl} onChange={(e) => setOrderUrl(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="part-cost">Cost</Label>
            <Input id="part-cost" type="number" step="0.01" value={cost} onChange={(e) => setCost(e.target.value)} />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!name.trim()}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
