import { useState } from "react";
import { MoreHorizontal, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  useCreateDashboard,
  useDeleteDashboard,
  useUpdateDashboard,
} from "@/hooks/useDashboardMutations";
import type { DashboardSummaryResponse } from "@/api/types";
import { toast } from "sonner";
import { DashboardSelector } from "./DashboardSelector";

interface DashboardManagerBarProps {
  householdId: string;
  dashboards: DashboardSummaryResponse[];
  activeDashboardId: string;
  canEdit: boolean;
  onSelect: (id: string) => void;
  onDeleted: (deletedId: string) => void;
}

type ModalState =
  | { kind: "none" }
  | { kind: "create" }
  | { kind: "rename"; dashboard: DashboardSummaryResponse }
  | { kind: "delete"; dashboard: DashboardSummaryResponse };

export function DashboardManagerBar({
  householdId,
  dashboards,
  activeDashboardId,
  canEdit,
  onSelect,
  onDeleted,
}: DashboardManagerBarProps) {
  const [modal, setModal] = useState<ModalState>({ kind: "none" });
  const [nameInput, setNameInput] = useState("");

  const createMutation = useCreateDashboard(householdId);
  const deleteMutation = useDeleteDashboard(householdId);

  const activeDashboard = dashboards.find((d) => d.id === activeDashboardId);
  const updateMutation = useUpdateDashboard(householdId, activeDashboardId);

  function openCreate() {
    setNameInput("");
    setModal({ kind: "create" });
  }

  function openRename(dashboard: DashboardSummaryResponse) {
    setNameInput(dashboard.name);
    setModal({ kind: "rename", dashboard });
  }

  function openDelete(dashboard: DashboardSummaryResponse) {
    setModal({ kind: "delete", dashboard });
  }

  function close() {
    setModal({ kind: "none" });
  }

  async function handleCreate() {
    const name = nameInput.trim();
    if (!name) return;
    try {
      const created = await createMutation.mutateAsync({ name });
      close();
      toast.success(`Dashboard "${created.name}" created.`);
      onSelect(created.id);
    } catch {
      toast.error("Failed to create dashboard.");
    }
  }

  async function handleRename() {
    if (modal.kind !== "rename" || !activeDashboard) return;
    const name = nameInput.trim();
    if (!name) return;
    try {
      await updateMutation.mutateAsync({
        name,
        isDefault: activeDashboard.isDefault,
        position: activeDashboard.position,
      });
      close();
      toast.success("Dashboard renamed.");
    } catch {
      toast.error("Failed to rename dashboard.");
    }
  }

  async function handleMakeDefault() {
    if (!activeDashboard) return;
    try {
      await updateMutation.mutateAsync({
        name: activeDashboard.name,
        isDefault: true,
        position: activeDashboard.position,
      });
      toast.success(`"${activeDashboard.name}" is now the default dashboard.`);
    } catch {
      toast.error("Failed to update dashboard.");
    }
  }

  async function handleDelete() {
    if (modal.kind !== "delete") return;
    const id = modal.dashboard.id;
    try {
      await deleteMutation.mutateAsync(id);
      close();
      toast.success(`Dashboard "${modal.dashboard.name}" deleted.`);
      onDeleted(id);
    } catch (err: unknown) {
      const msg =
        err instanceof Error ? err.message : "Failed to delete dashboard.";
      toast.error(msg);
    }
  }

  const isPending =
    createMutation.isPending || updateMutation.isPending || deleteMutation.isPending;

  return (
    <>
      <div className="flex items-center gap-2">
        <DashboardSelector
          dashboards={dashboards}
          activeDashboardId={activeDashboardId}
          householdId={householdId}
          onSelect={onSelect}
        />
        {canEdit && (
          <>
            <Button variant="ghost" size="icon" onClick={openCreate} title="New dashboard">
              <Plus className="h-4 w-4" />
            </Button>
            {activeDashboard && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon" title="Manage dashboard">
                    <MoreHorizontal className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="start">
                  <DropdownMenuItem onSelect={() => openRename(activeDashboard)}>
                    Rename
                  </DropdownMenuItem>
                  {!activeDashboard.isDefault && (
                    <DropdownMenuItem onSelect={handleMakeDefault}>
                      Make default
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onSelect={() => openDelete(activeDashboard)}
                    className="text-destructive focus:text-destructive"
                  >
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </>
        )}
      </div>

      {/* Create dialog */}
      <Dialog open={modal.kind === "create"} onOpenChange={(o) => !o && close()}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>New Dashboard</DialogTitle>
          </DialogHeader>
          <div className="space-y-2 py-1">
            <Label htmlFor="dashboard-name">Name</Label>
            <Input
              id="dashboard-name"
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleCreate()}
              autoFocus
              maxLength={100}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={close}>Cancel</Button>
            <Button onClick={handleCreate} disabled={!nameInput.trim() || isPending}>
              {isPending ? "Creating…" : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Rename dialog */}
      <Dialog open={modal.kind === "rename"} onOpenChange={(o) => !o && close()}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Rename Dashboard</DialogTitle>
          </DialogHeader>
          <div className="space-y-2 py-1">
            <Label htmlFor="rename-name">Name</Label>
            <Input
              id="rename-name"
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleRename()}
              autoFocus
              maxLength={100}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={close}>Cancel</Button>
            <Button onClick={handleRename} disabled={!nameInput.trim() || isPending}>
              {isPending ? "Saving…" : "Save"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete confirm dialog */}
      <Dialog open={modal.kind === "delete"} onOpenChange={(o) => !o && close()}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete Dashboard</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground py-1">
            Delete &ldquo;{modal.kind === "delete" ? modal.dashboard.name : ""}&rdquo;? This cannot
            be undone.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={close}>Cancel</Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isPending}>
              {isPending ? "Deleting…" : "Delete"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
