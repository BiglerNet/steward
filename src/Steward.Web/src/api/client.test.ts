import type { AxiosRequestConfig, AxiosResponse } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient } from "@/api/client";
import { readSession, writeSession, type StoredSession } from "@/lib/session";
import * as sessionRefreshModule from "@/lib/sessionRefresh";

vi.mock("@/lib/sessionRefresh");

const futureIso = new Date(Date.now() + 3_600_000).toISOString();

function seedSession(overrides: Partial<StoredSession> = {}): StoredSession {
  const session: StoredSession = {
    token: "old-token",
    refreshToken: "old-refresh-token",
    expiresAt: futureIso,
    user: { id: "user-1", email: "user@example.com", displayName: null, themePreference: null },
    pendingInvites: [],
    ...overrides,
  };
  writeSession(session);
  return session;
}

function unauthorizedError(config: AxiosRequestConfig) {
  return Object.assign(new Error("Unauthorized"), {
    isAxiosError: true,
    config,
    response: { status: 401, data: {}, headers: {}, config, statusText: "Unauthorized" },
  });
}

const originalAdapter = apiClient.defaults.adapter;

describe("apiClient response interceptor", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
  });

  it("retries a 401 once after a silent refresh, using the new access token", async () => {
    seedSession();
    const rotated: StoredSession = {
      token: "new-token",
      refreshToken: "new-refresh-token",
      expiresAt: futureIso,
      user: { id: "user-1", email: "user@example.com", displayName: null, themePreference: null },
      pendingInvites: [],
    };
    vi.mocked(sessionRefreshModule.refreshSession).mockImplementation(async () => {
      writeSession(rotated);
      return rotated;
    });

    let callCount = 0;
    apiClient.defaults.adapter = async (config): Promise<AxiosResponse> => {
      callCount += 1;
      if (callCount === 1) {
        throw unauthorizedError(config);
      }
      return {
        status: 200,
        statusText: "OK",
        data: { authHeader: config.headers?.Authorization },
        headers: {},
        config,
      };
    };

    const response = await apiClient.get("/api/some-protected-endpoint");

    expect(callCount).toBe(2);
    expect(sessionRefreshModule.refreshSession).toHaveBeenCalledTimes(1);
    expect(response.data.authHeader).toBe("Bearer new-token");
    expect(readSession()?.token).toBe("new-token");
  });

  it("clears the session when the refresh attempt fails", async () => {
    seedSession();
    vi.mocked(sessionRefreshModule.refreshSession).mockResolvedValue(null);

    apiClient.defaults.adapter = async (config): Promise<AxiosResponse> => {
      throw unauthorizedError(config);
    };

    await expect(apiClient.get("/api/some-protected-endpoint")).rejects.toThrow();

    expect(readSession()).toBeNull();
  });

  it("does not attempt a second refresh when the retried request also 401s", async () => {
    seedSession();
    vi.mocked(sessionRefreshModule.refreshSession).mockResolvedValue({
      token: "new-token",
      refreshToken: "new-refresh-token",
      expiresAt: futureIso,
      user: { id: "user-1", email: "user@example.com", displayName: null, themePreference: null },
      pendingInvites: [],
    });

    apiClient.defaults.adapter = async (config): Promise<AxiosResponse> => {
      throw unauthorizedError(config);
    };

    await expect(apiClient.get("/api/some-protected-endpoint")).rejects.toThrow();

    expect(sessionRefreshModule.refreshSession).toHaveBeenCalledTimes(1);
    expect(readSession()).toBeNull();
  });
});
