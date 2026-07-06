import type { ThemePreference } from "@/api/types";

const THEME_KEY = "mt.themePreference";

export function readLocalThemePreference(): ThemePreference | null {
  const raw = localStorage.getItem(THEME_KEY);
  return raw === "Light" || raw === "Dark" || raw === "System" ? raw : null;
}

export function writeLocalThemePreference(preference: ThemePreference): void {
  localStorage.setItem(THEME_KEY, preference);
}

export function prefersDarkColorScheme(): boolean {
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}
