import { useState } from "react";
import { useNavigate } from "react-router";
import { toast } from "sonner";
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
import { TemplatePicker } from "@/components/templates/TemplatePicker";
import { useCreateMaintenanceItem } from "@/hooks/useMaintenanceItemMutations";
import type { AssetCategory } from "@/api/types";

interface QuickCreateMaintenanceItemDialogProps {
  householdId: string;
  assetId: string;
  assetCategory: AssetCategory;
}

export function QuickCreateMaintenanceItemDialog({
  householdId,
  assetId,
  assetCategory,
}: QuickCreateMaintenanceItemDialogProps) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [templateId, setTemplateId] = useState<string | null>(null);
  const navigate = useNavigate();
  const createMutation = useCreateMaintenanceItem(householdId, assetId);

  function reset() {
    setTitle("");
    setTemplateId(null);
  }

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!title.trim()) return;

    createMutation.mutate(
      { title: title.trim(), templateId: templateId ?? undefined },
      {
        onSuccess: (item) => {
          setOpen(false);
          reset();
          navigate(`/households/${householdId}/assets/${assetId}/maintenance/${item.id}`);
        },
        onError: () => toast.error("Couldn't create this maintenance item."),
      }
    );
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) reset();
      }}
    >
      <DialogTrigger asChild>
        <Button size="sm">New</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New maintenance item</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="maintenance-title">Title</Label>
            <Input
              id="maintenance-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Oil change"
              autoFocus
            />
          </div>
          <div className="space-y-2">
            <Label>Template (optional)</Label>
            <TemplatePicker
              householdId={householdId}
              assetCategory={assetCategory}
              value={templateId}
              onChange={setTemplateId}
            />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!title.trim() || createMutation.isPending}>
              {createMutation.isPending ? "Creating…" : "Create"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
