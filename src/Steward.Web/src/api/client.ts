import axios from "axios";
import { toast } from "sonner";
import { clearSession, readSession } from "@/lib/session";

const apiBaseUrl = window.__APP_CONFIG__?.apiBaseUrl ?? import.meta.env.VITE_API_BASE_URL;

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

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;

    if (status === 401) {
      clearSession();
      if (window.location.pathname !== "/login") {
        window.location.assign("/login");
      }
      return Promise.reject(error);
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
