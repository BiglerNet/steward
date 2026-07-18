import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation, useNavigate, useParams } from "react-router";
import { toast } from "sonner";
import { getAsset } from "@/api/assets";
import { ChecklistSection } from "@/components/maintenance/ChecklistSection";
import { DoneTransitionDialog } from "@/components/maintenance/DoneTransitionDialog";
import { PartsSection } from "@/components/maintenance/PartsSection";
import { MarkdownContent } from "@/components/markdown/MarkdownContent";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useEngines } from "@/hooks/useEngines";
import { useDeleteMaintenanceItem, useMaintenanceItemMutations } from "@/hooks/useMaintenanceItemMutations";
import { useMaintenanceItem } from "@/hooks/useMaintenanceItems";
import { numberToInputValue, parseOptionalNumber, parseOptionalText, textToInputValue } from "@/lib/formHelpers";
import { useHouseholdRole } from "@/lib/permissions";
import type { ChecklistItemStatus, MaintenanceItemStatus, PartLineStatus } from "@/api/types";

const STATUSES: MaintenanceItemStatus[] = ["Planned", "InProgress", "Done", "Cancelled"];

export function MaintenanceItemEditorPage() {
  const { householdId, assetId, itemId } = useParams() as {
    householdId: string;
    assetId: string;
    itemId: string;
  };
  const navigate = useNavigate();
  const location = useLocation();
  const navState = location.state as { from?: string; fromLabel?: string } | null;
  const { canEdit } = useHouseholdRole();
  const { data: item } = useMaintenanceItem(householdId, assetId, itemId);
  const { data: asset } = useQuery({
    queryKey: ["households", householdId, "assets", assetId],
    queryFn: () => getAsset(householdId, assetId),
    enabled: !navState?.from,
  });
  const { data: engines } = useEngines(householdId, assetId);
  const mutations = useMaintenanceItemMutations(householdId, assetId, itemId);
  const deleteMutation = useDeleteMaintenanceItem(householdId, assetId);

  const [title, setTitle] = useState("");
  const [providerName, setProviderName] = useState("");
  const [description, setDescription] = useState("");
  const [date, setDate] = useState("");
  const [cost, setCost] = useState("");
  const [odometerMiles, setOdometerMiles] = useState("");
  const [engineHours, setEngineHours] = useState("");
  const [engineId, setEngineId] = useState("");
  const [pendingDoneTransition, setPendingDoneTransition] = useState(false);
  const [transitionPending, setTransitionPending] = useState(false);

  // Re-sync local field state when the loaded item's identity changes (initial load, or
  // navigating between items) without clobbering mid-edit state on a background refetch.
  // This is React's documented "adjust state during render" pattern, not an effect, so a
  // server refetch that doesn't change `item.id` never resets fields the user is editing.
  const [syncedItemId, setSyncedItemId] = useState<string | null>(null);
  if (item && item.id !== syncedItemId) {
    setSyncedItemId(item.id);
    setTitle(item.title);
    setProviderName(textToInputValue(item.providerName));
    setDescription(item.description ?? "");
    setDate(textToInputValue(item.date));
    setCost(numberToInputValue(item.cost));
    setOdometerMiles(numberToInputValue(item.odometerMiles));
    setEngineHours(numberToInputValue(item.engineHours));
    setEngineId(textToInputValue(item.engineId));
  }

  if (!item) {
    return null;
  }

  function patch(field: Parameters<typeof mutations.patchItem.mutate>[0]) {
    mutations.patchItem.mutate(field, { onError: () => toast.error("Couldn't save this field.") });
  }

  function handleStatusChange(nextStatus: MaintenanceItemStatus) {
    if (nextStatus === "Done") {
      const openItems = item!.checklistItems.filter((c) => c.status === "Open");
      if (openItems.length > 0) {
        setPendingDoneTransition(true);
        return;
      }
    }
    patch({ status: nextStatus });
  }

  async function handleCompleteAnyway() {
    setTransitionPending(true);
    try {
      await mutations.patchItem.mutateAsync({ status: "Done" });
      setPendingDoneTransition(false);
    } catch {
      toast.error("Couldn't complete this item.");
    } finally {
      setTransitionPending(false);
    }
  }

  async function handleSkipRemainingThenComplete() {
    setTransitionPending(true);
    try {
      const openItems = item!.checklistItems.filter((c) => c.status === "Open");
      await Promise.all(
        openItems.map((c) => mutations.editChecklistItem.mutateAsync({ checklistItemId: c.id, request: { status: "Skipped" } }))
      );
      await mutations.patchItem.mutateAsync({ status: "Done" });
      setPendingDoneTransition(false);
    } catch {
      toast.error("Couldn't complete this item.");
    } finally {
      setTransitionPending(false);
    }
  }

  function handleDelete() {
    if (window.confirm(`Delete "${item!.title}"? This can't be undone.`)) {
      deleteMutation.mutate(item!.id, {
        onSuccess: () => navigate(`/households/${householdId}/assets/${assetId}/maintenance`),
        onError: () => toast.error("Couldn't delete this maintenance item."),
      });
    }
  }

  function handleChecklistSetStatus(checklistItemId: string, status: ChecklistItemStatus) {
    mutations.editChecklistItem.mutate(
      { checklistItemId, request: { status } },
      { onError: () => toast.error("Couldn't update this checklist item.") }
    );
  }

  function handleChecklistMove(checklistItemId: string, direction: "up" | "down") {
    const ids = item!.checklistItems.map((c) => c.id);
    const index = ids.indexOf(checklistItemId);
    const swapWith = direction === "up" ? index - 1 : index + 1;
    if (swapWith < 0 || swapWith >= ids.length) return;
    const next = [...ids];
    [next[index], next[swapWith]] = [next[swapWith], next[index]];
    mutations.reorderChecklist.mutate(next, { onError: () => toast.error("Couldn't reorder checklist.") });
  }

  const backTo = navState?.from ?? `/households/${householdId}/assets/${assetId}/maintenance`;

  return (
    <div className="space-y-6">
      <nav aria-label="Breadcrumb" className="flex items-center gap-1 text-sm text-muted-foreground">
        {navState?.fromLabel ? (
          <Link to={backTo} className="text-primary underline-offset-4 hover:underline">
            {navState.fromLabel}
          </Link>
        ) : (
          <>
            <Link to={backTo} className="text-primary underline-offset-4 hover:underline">
              {asset?.name ?? "Asset"}
            </Link>
            <span>›</span>
            <Link to={backTo} className="text-primary underline-offset-4 hover:underline">
              Maintenance
            </Link>
          </>
        )}
        <span>›</span>
        <span className="truncate">{item.title}</span>
      </nav>

      <div className="flex items-center justify-between">
        {canEdit ? (
          <Input
            className="text-h1 h-auto border-none px-0 shadow-none focus-visible:ring-0"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => title.trim() && title !== item.title && patch({ title: title.trim() })}
          />
        ) : (
          <h1 className="text-h1">{item.title}</h1>
        )}
        {canEdit && (
          <Button variant="destructive" onClick={handleDelete} disabled={deleteMutation.isPending}>
            Delete
          </Button>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div className="space-y-2">
          <Label>Status</Label>
          {canEdit ? (
            <Select value={item.status} onValueChange={(v) => handleStatusChange(v as MaintenanceItemStatus)}>
              <SelectTrigger aria-label="Status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {STATUSES.map((status) => (
                  <SelectItem key={status} value={status}>
                    {status}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : (
            <p className="text-sm">{item.status}</p>
          )}
        </div>

        {item.completedAt && (
          <div className="space-y-2">
            <Label>Completed</Label>
            <p className="text-sm">{new Date(item.completedAt).toLocaleDateString()}</p>
          </div>
        )}

        <div className="space-y-2">
          <Label htmlFor="maintenance-date">Date</Label>
          {canEdit ? (
            <Input
              id="maintenance-date"
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              onBlur={() => date !== textToInputValue(item.date) && patch({ date: parseOptionalText(date) })}
            />
          ) : (
            <p className="text-sm">{item.date ?? "—"}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="maintenance-cost">Cost</Label>
          {canEdit ? (
            <Input
              id="maintenance-cost"
              type="number"
              step="0.01"
              value={cost}
              onChange={(e) => setCost(e.target.value)}
              onBlur={() => cost !== numberToInputValue(item.cost) && patch({ cost: parseOptionalNumber(cost) })}
            />
          ) : (
            <p className="text-sm">{item.cost ?? "—"}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="maintenance-odometer">Odometer (miles)</Label>
          {canEdit ? (
            <Input
              id="maintenance-odometer"
              type="number"
              step="0.1"
              value={odometerMiles}
              onChange={(e) => setOdometerMiles(e.target.value)}
              onBlur={() =>
                odometerMiles !== numberToInputValue(item.odometerMiles) &&
                patch({ odometerMiles: parseOptionalNumber(odometerMiles) })
              }
            />
          ) : (
            <p className="text-sm">{item.odometerMiles ?? "—"}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="maintenance-engine-hours">Engine hours</Label>
          {canEdit ? (
            <Input
              id="maintenance-engine-hours"
              type="number"
              step="0.1"
              value={engineHours}
              onChange={(e) => setEngineHours(e.target.value)}
              onBlur={() =>
                engineHours !== numberToInputValue(item.engineHours) &&
                patch({ engineHours: parseOptionalNumber(engineHours) })
              }
            />
          ) : (
            <p className="text-sm">{item.engineHours ?? "—"}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label>Engine</Label>
          {canEdit ? (
            <Select
              value={engineId || "none"}
              onValueChange={(v) => {
                const next = v === "none" ? "" : v;
                setEngineId(next);
                patch({ engineId: next || null });
              }}
            >
              <SelectTrigger aria-label="Engine">
                <SelectValue placeholder="No engine" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="none">No engine</SelectItem>
                {engines?.map((engine) => (
                  <SelectItem key={engine.id} value={engine.id}>
                    {engine.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          ) : (
            <p className="text-sm">{engines?.find((e) => e.id === item.engineId)?.label ?? "—"}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="maintenance-provider">Provider</Label>
          {canEdit ? (
            <Input
              id="maintenance-provider"
              value={providerName}
              onChange={(e) => setProviderName(e.target.value)}
              onBlur={() =>
                providerName !== textToInputValue(item.providerName) &&
                patch({ providerName: parseOptionalText(providerName) })
              }
            />
          ) : (
            <p className="text-sm">{item.providerName ?? "—"}</p>
          )}
        </div>
      </div>

      <div className="space-y-2">
        <Label>Description</Label>
        {canEdit ? (
          <MarkdownEditor
            value={description}
            onChange={setDescription}
            onBlur={() => description !== (item.description ?? "") && patch({ description: description || null })}
          />
        ) : item.description ? (
          <MarkdownContent>{item.description}</MarkdownContent>
        ) : (
          <p className="text-sm text-muted-foreground">No description.</p>
        )}
      </div>

      <ChecklistSection
        items={item.checklistItems}
        engines={engines ?? []}
        canEdit={canEdit}
        onSetStatus={handleChecklistSetStatus}
        onMove={handleChecklistMove}
        onReorder={(ids) => mutations.reorderChecklist.mutate(ids, { onError: () => toast.error("Couldn't reorder checklist.") })}
        onAdd={(text) => mutations.addChecklistItem.mutate({ text }, { onError: () => toast.error("Couldn't add checklist item.") })}
        onDelete={(id) => mutations.removeChecklistItem.mutate(id, { onError: () => toast.error("Couldn't delete checklist item.") })}
      />

      <PartsSection
        partLines={item.partLines}
        canEdit={canEdit}
        onAdd={(request) => mutations.addPartLine.mutate(request, { onError: () => toast.error("Couldn't add part.") })}
        onEdit={(id, request) =>
          mutations.editPartLine.mutate({ partLineId: id, request }, { onError: () => toast.error("Couldn't update part.") })
        }
        onSetStatus={(id, status: PartLineStatus) =>
          mutations.editPartLine.mutate(
            { partLineId: id, request: { status } },
            { onError: () => toast.error("Couldn't update part.") }
          )
        }
        onDelete={(id) => mutations.removePartLine.mutate(id, { onError: () => toast.error("Couldn't delete part.") })}
      />

      <DoneTransitionDialog
        open={pendingDoneTransition}
        openItemCount={item.checklistItems.filter((c) => c.status === "Open").length}
        pending={transitionPending}
        onGoBack={() => setPendingDoneTransition(false)}
        onCompleteAnyway={handleCompleteAnyway}
        onSkipRemainingThenComplete={handleSkipRemainingThenComplete}
      />
    </div>
  );
}
