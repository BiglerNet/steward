import type { HouseholdMaintenanceItemResponse, MaintenanceItemStatus } from "@/api/types";

export type DropPlan =
  | { type: "noop" }
  | { type: "patch"; status: MaintenanceItemStatus }
  | { type: "confirmDone" };

export function planDrop(
  item: HouseholdMaintenanceItemResponse,
  targetStatus: MaintenanceItemStatus
): DropPlan {
  if (targetStatus === item.status) return { type: "noop" };
  if (targetStatus === "Done") {
    const hasOpenItems = item.checklistItems.some((c) => c.status === "Open");
    return hasOpenItems ? { type: "confirmDone" } : { type: "patch", status: "Done" };
  }
  return { type: "patch", status: targetStatus };
}

export const RECENTLY_DONE_WINDOW_DAYS = 7;

export function isRecentlyCompleted(item: HouseholdMaintenanceItemResponse, now = new Date()): boolean {
  if (item.status !== "Done" || !item.completedAt) return false;
  const completedAt = new Date(item.completedAt);
  const daysSince = (now.getTime() - completedAt.getTime()) / (1000 * 60 * 60 * 24);
  return daysSince >= 0 && daysSince <= RECENTLY_DONE_WINDOW_DAYS;
}
