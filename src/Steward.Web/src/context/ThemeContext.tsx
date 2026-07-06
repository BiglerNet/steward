import { createContext, useContext, useEffect, useLayoutEffect, useState, type ReactNode } from "react";
import { useAuth } from "@/context/AuthContext";
import type { ThemePreference } from "@/api/types";
import { prefersDarkColorScheme, readLocalThemePreference, writeLocalThemePreference } from "@/lib/theme";

type ResolvedTheme = "light" | "dark";

interface ThemeContextValue {
  themePreference: ThemePreference;
  resolvedTheme: ResolvedTheme;
  setThemePreference: (preference: ThemePreference) => void;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const { user, isAuthenticated, updateThemePreference: persistThemePreference } = useAuth();

  const [localPreference, setLocalPreference] = useState<ThemePreference | null>(() =>
    readLocalThemePreference()
  );
  const [systemPrefersDark, setSystemPrefersDark] = useState<boolean>(() => prefersDarkColorScheme());

  useEffect(() => {
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    function handleChange(event: MediaQueryListEvent) {
      setSystemPrefersDark(event.matches);
    }
    media.addEventListener("change", handleChange);
    return () => media.removeEventListener("change", handleChange);
  }, []);

  // An authenticated user's stored preference is the source of truth once known;
  // otherwise fall back to this device's local choice, then "System".
  const themePreference: ThemePreference = (isAuthenticated ? user?.themePreference : null) ?? localPreference ?? "System";

  const resolvedTheme: ResolvedTheme =
    themePreference === "System" ? (systemPrefersDark ? "dark" : "light") : themePreference === "Dark" ? "dark" : "light";

  // useLayoutEffect (not useEffect) so the class is applied before the browser paints, avoiding a flash of the wrong theme.
  useLayoutEffect(() => {
    document.documentElement.classList.toggle("dark", resolvedTheme === "dark");
  }, [resolvedTheme]);

  function setThemePreference(preference: ThemePreference) {
    setLocalPreference(preference);
    writeLocalThemePreference(preference);
    if (isAuthenticated) {
      void persistThemePreference(preference);
    }
  }

  const value: ThemeContextValue = { themePreference, resolvedTheme, setThemePreference };

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
}
