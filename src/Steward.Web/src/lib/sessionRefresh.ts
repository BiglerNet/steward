import axios from "axios";
import type { AuthResponse } from "@/api/types";
import { clearSession, readSession, writeSession, type StoredSession } from "@/lib/session";

const apiBaseUrl = window.__APP_CONFIG__?.apiBaseUrl ?? import.meta.env.VITE_API_BASE_URL;

let inFlightRefresh: Promise<StoredSession | null> | null = null;

// Uses a bare axios call (not apiClient) so this never re-enters apiClient's own
// response interceptor, which would otherwise recurse into this same refresh flow
// whenever /api/auth/refresh itself responds 401.
async function doRefresh(): Promise<StoredSession | null> {
  const session = readSession();
  if (!session?.refreshToken) {
    return null;
  }

  try {
    const { data } = await axios.post<AuthResponse>(
      `${apiBaseUrl}/api/auth/refresh`,
      { refreshToken: session.refreshToken },
      { headers: { "Content-Type": "application/json" } }
    );

    const nextSession: StoredSession = {
      token: data.token,
      refreshToken: data.refreshToken,
      expiresAt: data.expiresAt,
      user: data.user,
      pendingInvites: session.pendingInvites,
    };
    writeSession(nextSession);
    return nextSession;
  } catch {
    clearSession();
    return null;
  }
}

// Concurrent callers (a proactive timer tick racing a request-triggered refresh)
// share one in-flight request instead of each rotating the refresh token.
export function refreshSession(): Promise<StoredSession | null> {
  if (!inFlightRefresh) {
    inFlightRefresh = doRefresh().finally(() => {
      inFlightRefresh = null;
    });
  }
  return inFlightRefresh;
}
