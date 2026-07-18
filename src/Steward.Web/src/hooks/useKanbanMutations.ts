import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patchChecklistItem, patchMaintenanceItem } from "@/api/maintenanceItems";
import type { MaintenanceItemStatus } from "@/api/types";

interface ItemRef {
  assetId: string;
  itemId: string;
}

export function useKanbanMutations(householdId: string) {
  const queryClient = useQueryClient();

  function invalidate(assetId: string) {
    queryClient.invalidateQueries({ queryKey: ["households", householdId, "maintenance-items"] });
    queryClient.invalidateQueries({
      queryKey: ["households", householdId, "assets", assetId, "maintenance-items"],
    });
  }

  const patchStatus = useMutation({
    mutationFn: ({ assetId, itemId, status }: ItemRef & { status: MaintenanceItemStatus }) =>
      patchMaintenanceItem(householdId, assetId, itemId, { status }),
    onSuccess: (_data, variables) => invalidate(variables.assetId),
  });

  const skipChecklistItem = useMutation({
    mutationFn: ({ assetId, itemId, checklistItemId }: ItemRef & { checklistItemId: string }) =>
      patchChecklistItem(householdId, assetId, itemId, checklistItemId, { status: "Skipped" }),
  });

  return { patchStatus, skipChecklistItem };
}
