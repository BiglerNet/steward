import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import * as authApi from "@/api/auth";
import type {
  AuthenticatedUser,
  AuthResponse,
  LoginRequest,
  OAuthExchangeRequest,
  PendingInviteSummary,
  RegisterRequest,
  ThemePreference,
} from "@/api/types";
import { clearSession, readSession, SESSION_KEY, writeSession, type StoredSession } from "@/lib/session";
import { refreshSession } from "@/lib/sessionRefresh";

// Fire the proactive refresh this long before the access token's actual expiry,
// so it lands well inside the token's remaining lifetime rather than racing it.
const REFRESH_BUFFER_MS = 60_000;

interface AuthContextValue {
  user: AuthenticatedUser | null;
  token: string | null;
  expiresAt: string | null;
  pendingInvites: PendingInviteSummary[];
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  exchangeOAuthCode: (request: OAuthExchangeRequest) => Promise<void>;
  logout: () => Promise<void>;
  removePendingInvite: (inviteCode: string) => void;
  updateThemePreference: (themePreference: ThemePreference) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function redirectToLogin() {
  if (window.location.pathname !== "/login") {
    window.location.assign("/login");
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const initialSession = readSession();

  const [user, setUser] = useState<AuthenticatedUser | null>(initialSession?.user ?? null);
  const [token, setToken] = useState<string | null>(initialSession?.token ?? null);
  const [expiresAt, setExpiresAt] = useState<string | null>(initialSession?.expiresAt ?? null);
  const [pendingInvites, setPendingInvites] = useState<PendingInviteSummary[]>(
    initialSession?.pendingInvites ?? []
  );

  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  function clearScheduledRefresh() {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
      refreshTimerRef.current = null;
    }
  }

  function applyStoredSession(next: StoredSession) {
    setUser(next.user);
    setToken(next.token);
    setExpiresAt(next.expiresAt);
    setPendingInvites(next.pendingInvites);
  }

  function clearLocalState() {
    setUser(null);
    setToken(null);
    setExpiresAt(null);
    setPendingInvites([]);
  }

  // performProactiveRefresh and scheduleRefresh reference each other to keep
  // rescheduling after every successful refresh. Plain function declarations
  // (hoisted within this render's closure) resolve that mutual reference
  // without the ref-to-latest-callback indirection React Compiler disallows.
  async function performProactiveRefresh() {
    const next = await refreshSession();
    if (next) {
      applyStoredSession(next);
      scheduleRefresh(next.expiresAt);
    } else {
      clearLocalState();
      redirectToLogin();
    }
  }

  function scheduleRefresh(expiresAtIso: string) {
    clearScheduledRefresh();
    const delay = Math.max(0, new Date(expiresAtIso).getTime() - Date.now() - REFRESH_BUFFER_MS);
    refreshTimerRef.current = setTimeout(() => {
      void performProactiveRefresh();
    }, delay);
  }

  useEffect(() => {
    const restored = readSession();
    if (restored?.expiresAt) {
      scheduleRefresh(restored.expiresAt);
    }

    function handleStorage(event: StorageEvent) {
      if (event.key !== SESSION_KEY) {
        return;
      }

      if (event.newValue === null) {
        clearScheduledRefresh();
        clearLocalState();
        redirectToLogin();
        return;
      }

      try {
        const next = JSON.parse(event.newValue) as StoredSession;
        applyStoredSession(next);
        scheduleRefresh(next.expiresAt);
      } catch {
        // Ignore malformed session payloads written by another tab.
      }
    }

    window.addEventListener("storage", handleStorage);
    return () => {
      clearScheduledRefresh();
      window.removeEventListener("storage", handleStorage);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function applySession(response: AuthResponse) {
    const next: StoredSession = {
      token: response.token,
      refreshToken: response.refreshToken,
      expiresAt: response.expiresAt,
      user: response.user,
      pendingInvites: response.pendingInvites,
    };
    writeSession(next);
    applyStoredSession(next);
    scheduleRefresh(next.expiresAt);
  }

  async function login(request: LoginRequest) {
    const response = await authApi.login(request);
    applySession(response);
  }

  async function register(request: RegisterRequest) {
    const response = await authApi.register(request);
    applySession(response);
  }

  async function exchangeOAuthCode(request: OAuthExchangeRequest) {
    const response = await authApi.exchangeOAuthCode(request);
    applySession(response);
  }

  async function logout() {
    const currentRefreshToken = readSession()?.refreshToken;
    clearScheduledRefresh();
    if (currentRefreshToken) {
      try {
        await authApi.logout({ refreshToken: currentRefreshToken });
      } catch {
        // Best-effort: proceed to clear the local session regardless.
      }
    }
    clearSession();
    clearLocalState();
  }

  function removePendingInvite(inviteCode: string) {
    setPendingInvites((current) => {
      const next = current.filter((invite) => invite.inviteCode !== inviteCode);
      const session = readSession();
      if (session) {
        writeSession({ ...session, pendingInvites: next });
      }
      return next;
    });
  }

  async function updateThemePreference(themePreference: ThemePreference) {
    const response = await authApi.updateThemePreference({ themePreference });
    setUser((current) => (current ? { ...current, themePreference: response.themePreference } : current));
    const session = readSession();
    if (session) {
      writeSession({
        ...session,
        user: { ...session.user, themePreference: response.themePreference },
      });
    }
  }

  const value: AuthContextValue = {
    user,
    token,
    expiresAt,
    pendingInvites,
    isAuthenticated: !!token,
    login,
    register,
    exchangeOAuthCode,
    logout,
    removePendingInvite,
    updateThemePreference,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
