import { apiClient } from "@/api/client";
import type {
  AuthResponse,
  LoginRequest,
  OAuthExchangeRequest,
  RegisterRequest,
  UserProfileResponse,
} from "@/api/types";

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/api/auth/register", request);
  return data;
}

export async function login(request: LoginRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/api/auth/login", request);
  return data;
}

export async function exchangeOAuthCode(request: OAuthExchangeRequest): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>("/api/auth/oauth/exchange", request);
  return data;
}

export async function me(): Promise<UserProfileResponse> {
  const { data } = await apiClient.get<UserProfileResponse>("/api/auth/me");
  return data;
}

export async function acceptInvite(code: string): Promise<void> {
  await apiClient.post(`/api/auth/invites/${encodeURIComponent(code)}/accept`);
}

export function oauthLoginUrl(provider: string, apiBaseUrl: string): string {
  return `${apiBaseUrl}/api/auth/oauth/${provider}/login`;
}
