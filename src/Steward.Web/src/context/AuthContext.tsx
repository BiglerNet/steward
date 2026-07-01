import { createContext, useContext, useState, type ReactNode } from "react";
import * as authApi from "@/api/auth";
import type {
  AuthenticatedUser,
  LoginRequest,
  OAuthExchangeRequest,
  PendingInviteSummary,
  RegisterRequest,
} from "@/api/types";
import { clearSession, readSession, writeSession } from "@/lib/session";

interface AuthContextValue {
  user: AuthenticatedUser | null;
  token: string | null;
  expiresAt: string | null;
  pendingInvites: PendingInviteSummary[];
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  exchangeOAuthCode: (request: OAuthExchangeRequest) => Promise<void>;
  logout: () => void;
  removePendingInvite: (inviteCode: string) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const initialSession = readSession();
  const [user, setUser] = useState<AuthenticatedUser | null>(initialSession?.user ?? null);
  const [token, setToken] = useState<string | null>(initialSession?.token ?? null);
  const [expiresAt, setExpiresAt] = useState<string | null>(initialSession?.expiresAt ?? null);
  const [pendingInvites, setPendingInvites] = useState<PendingInviteSummary[]>(
    initialSession?.pendingInvites ?? []
  );

  function applySession(response: {
    token: string;
    expiresAt: string;
    user: AuthenticatedUser;
    pendingInvites: PendingInviteSummary[];
  }) {
    writeSession(response);
    setUser(response.user);
    setToken(response.token);
    setExpiresAt(response.expiresAt);
    setPendingInvites(response.pendingInvites);
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

  function logout() {
    clearSession();
    setUser(null);
    setToken(null);
    setExpiresAt(null);
    setPendingInvites([]);
  }

  function removePendingInvite(inviteCode: string) {
    setPendingInvites((current) => {
      const next = current.filter((invite) => invite.inviteCode !== inviteCode);
      if (token && user && expiresAt) {
        writeSession({ token, expiresAt, user, pendingInvites: next });
      }
      return next;
    });
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
