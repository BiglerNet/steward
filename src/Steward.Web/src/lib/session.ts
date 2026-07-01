import type { AuthResponse } from "@/api/types";

const SESSION_KEY = "mt.session";
const LAST_HOUSEHOLD_KEY = "mt.lastHouseholdId";

export interface StoredSession {
  token: string;
  expiresAt: string;
  user: AuthResponse["user"];
  pendingInvites: AuthResponse["pendingInvites"];
}

export function readSession(): StoredSession | null {
  const raw = localStorage.getItem(SESSION_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    return null;
  }
}

export function writeSession(session: StoredSession): void {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session));
}

export function clearSession(): void {
  localStorage.removeItem(SESSION_KEY);
}

export function readLastHouseholdId(): string | null {
  return localStorage.getItem(LAST_HOUSEHOLD_KEY);
}

export function writeLastHouseholdId(householdId: string): void {
  localStorage.setItem(LAST_HOUSEHOLD_KEY, householdId);
}
