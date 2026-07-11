import axios, { type AxiosRequestConfig } from "axios";
import { toast } from "sonner";
import { clearSession, readSession } from "@/lib/session";
import { refreshSession } from "@/lib/sessionRefresh";

const apiBaseUrl = window.__APP_CONFIG__?.apiBaseUrl ?? import.meta.env.VITE_API_BASE_URL;
const REFRESH_PATH = "/api/auth/refresh";

interface RetryableRequestConfig extends AxiosRequestConfig {
  _retried?: boolean;
}

export const apiClient = axios.create({
  baseURL: apiBaseUrl,
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.request.use((config) => {
  const session = readSession();
  if (session?.token) {
    config.headers.Authorization = `Bearer ${session.token}`;
  }
  return config;
});

function redirectToLogin() {
  clearSession();
  if (window.location.pathname !== "/login") {
    window.location.assign("/login");
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const status = error.response?.status;
    const config = error.config as RetryableRequestConfig | undefined;

    if (status === 401) {
      // The refresh call itself 401ing, or a request we already retried once,
      // means refreshing won't help — fall back to the reactive logout.
      if (!config || config.url?.includes(REFRESH_PATH) || config._retried) {
        redirectToLogin();
        return Promise.reject(error);
      }

      const refreshed = await refreshSession();
      if (!refreshed) {
        redirectToLogin();
        return Promise.reject(error);
      }

      config._retried = true;
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${refreshed.token}`;
      return apiClient.request(config);
    }

    if (status === 403) {
      toast.error("You don't have permission to do that.");
    } else if (status === 404) {
      toast.error("That item couldn't be found.");
    } else if (status && status >= 500) {
      toast.error("Something went wrong. Please try again.");
    }

    return Promise.reject(error);
  }
);
