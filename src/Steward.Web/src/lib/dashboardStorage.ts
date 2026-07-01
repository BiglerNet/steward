const KEY_PREFIX = "dashboard";

export function readLastDashboardId(householdId: string): string | null {
  return localStorage.getItem(`${KEY_PREFIX}:${householdId}`);
}

export function writeLastDashboardId(householdId: string, dashboardId: string): void {
  localStorage.setItem(`${KEY_PREFIX}:${householdId}`, dashboardId);
}
