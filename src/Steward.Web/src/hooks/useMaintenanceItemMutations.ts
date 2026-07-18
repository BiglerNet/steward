import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createChecklistItem,
  createMaintenanceItem,
  createPartLine,
  deleteChecklistItem,
  deleteMaintenanceItem,
  deletePartLine,
  patchChecklistItem,
  patchMaintenanceItem,
  patchPartLine,
  reorderChecklistItems,
} from "@/api/maintenanceItems";
import type {
  CreateChecklistItemRequest,
  CreateMaintenanceItemRequest,
  CreatePartLineRequest,
  MaintenanceItemResponse,
  PatchChecklistItemRequest,
  PatchMaintenanceItemRequest,
  PatchPartLineRequest,
} from "@/api/types";

function listKey(householdId: string, assetId: string) {
  return ["households", householdId, "assets", assetId, "maintenance-items"];
}

function itemKey(householdId: string, assetId: string, maintenanceItemId: string) {
  return ["households", householdId, "assets", assetId, "maintenance-items", maintenanceItemId];
}

export function useCreateMaintenanceItem(householdId: string, assetId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateMaintenanceItemRequest) =>
      createMaintenanceItem(householdId, assetId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
    },
  });
}

export function useDeleteMaintenanceItem(householdId: string, assetId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (maintenanceItemId: string) =>
      deleteMaintenanceItem(householdId, assetId, maintenanceItemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
    },
  });
}

/**
 * Mutations for a single maintenance item's full-page editor: patching the item itself and
 * managing its checklist items and part lines. Every mutation writes the fresh item straight
 * into the detail query cache (avoiding a refetch round-trip for every autosave) and
 * invalidates the list query, since list rows show status/date/cost/isBlocked.
 */
export function useMaintenanceItemMutations(householdId: string, assetId: string, maintenanceItemId: string) {
  const queryClient = useQueryClient();
  const key = itemKey(householdId, assetId, maintenanceItemId);

  function applyItem(item: MaintenanceItemResponse) {
    queryClient.setQueryData(key, item);
    queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
  }

  const patchItem = useMutation({
    mutationFn: (request: PatchMaintenanceItemRequest) =>
      patchMaintenanceItem(householdId, assetId, maintenanceItemId, request),
    onSuccess: applyItem,
  });

  const addChecklistItem = useMutation({
    mutationFn: (request: CreateChecklistItemRequest) =>
      createChecklistItem(householdId, assetId, maintenanceItemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key }),
  });

  const editChecklistItem = useMutation({
    mutationFn: ({ checklistItemId, request }: { checklistItemId: string; request: PatchChecklistItemRequest }) =>
      patchChecklistItem(householdId, assetId, maintenanceItemId, checklistItemId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key }),
  });

  const removeChecklistItem = useMutation({
    mutationFn: (checklistItemId: string) =>
      deleteChecklistItem(householdId, assetId, maintenanceItemId, checklistItemId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key }),
  });

  const reorderChecklist = useMutation({
    mutationFn: (checklistItemIds: string[]) =>
      reorderChecklistItems(householdId, assetId, maintenanceItemId, checklistItemIds),
    // Optimistic reorder: without this, the checklist briefly re-renders in its pre-drag
    // order (the cache hasn't changed yet) before the invalidated query refetches and
    // re-sorts it, which reads as the dropped item snapping back then jumping to place.
    // Writing the new order into the cache immediately keeps the drop visually stable.
    onMutate: async (checklistItemIds: string[]) => {
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<MaintenanceItemResponse>(key);

      if (previous) {
        const byId = new Map(previous.checklistItems.map((item) => [item.id, item]));
        const reordered = checklistItemIds
          .map((id, index) => {
            const item = byId.get(id);
            return item ? { ...item, sortOrder: index } : null;
          })
          .filter((item) => item !== null);
        queryClient.setQueryData(key, { ...previous, checklistItems: reordered });
      }

      return { previous };
    },
    onError: (_error, _checklistItemIds, context) => {
      if (context?.previous) {
        queryClient.setQueryData(key, context.previous);
      }
    },
    onSuccess: (reordered) => {
      queryClient.setQueryData<MaintenanceItemResponse | undefined>(key, (current) =>
        current ? { ...current, checklistItems: reordered } : current
      );
    },
  });

  const addPartLine = useMutation({
    mutationFn: (request: CreatePartLineRequest) =>
      createPartLine(householdId, assetId, maintenanceItemId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: key });
      queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
    },
  });

  const editPartLine = useMutation({
    mutationFn: ({ partLineId, request }: { partLineId: string; request: PatchPartLineRequest }) =>
      patchPartLine(householdId, assetId, maintenanceItemId, partLineId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: key });
      queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
    },
  });

  const removePartLine = useMutation({
    mutationFn: (partLineId: string) => deletePartLine(householdId, assetId, maintenanceItemId, partLineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: key });
      queryClient.invalidateQueries({ queryKey: listKey(householdId, assetId) });
    },
  });

  return {
    patchItem,
    addChecklistItem,
    editChecklistItem,
    removeChecklistItem,
    reorderChecklist,
    addPartLine,
    editPartLine,
    removePartLine,
  };
}
