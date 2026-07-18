import { QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { Toaster } from "@/components/ui/sonner";
import { EnginesSection } from "@/components/assets/EnginesSection";
import { AuthenticatedLayout } from "@/components/layout/AuthenticatedLayout";
import { AuthProvider } from "@/context/AuthContext";
import { ThemeProvider } from "@/context/ThemeContext";
import { queryClient } from "@/lib/queryClient";
import { AuthCallbackPage } from "@/pages/AuthCallbackPage";
import { AssetCreateWizardPage } from "@/pages/assets/AssetCreateWizardPage";
import { AssetDetailLayout } from "@/pages/assets/AssetDetailLayout";
import { AssetListPage } from "@/pages/assets/AssetListPage";
import { EngineHoursLogsPage } from "@/pages/assets/EngineHoursLogsPage";
import { FuelLogsPage } from "@/pages/assets/FuelLogsPage";
import { MaintenanceItemsPage } from "@/pages/assets/MaintenanceItemsPage";
import { MaintenanceItemEditorPage } from "@/pages/maintenance/MaintenanceItemEditorPage";
import { MaintenanceKanbanPage } from "@/pages/maintenance/MaintenanceKanbanPage";
import { MileageLogsPage } from "@/pages/assets/MileageLogsPage";
import { RegistrationsSection } from "@/components/registrations/RegistrationsSection";
import { WarrantiesSection } from "@/components/warranties/WarrantiesSection";
import { DashboardPage } from "@/pages/DashboardPage";
import { HouseholdSettingsPage } from "@/pages/HouseholdSettingsPage";
import { HouseholdsIndexPage } from "@/pages/HouseholdsIndexPage";
import { LoginPage } from "@/pages/LoginPage";
import { PendingInvitesPage } from "@/pages/PendingInvitesPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { AdminShellPage } from "@/pages/admin/AdminShellPage";
import { AdminTemplatesPage } from "@/pages/admin/AdminTemplatesPage";
import { PlatformAdminRoute } from "@/routes/PlatformAdminRoute";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { PublicOnlyRoute } from "@/routes/PublicOnlyRoute";

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <ThemeProvider>
            <Routes>
              <Route element={<PublicOnlyRoute />}>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
              </Route>
              <Route path="/auth/callback" element={<AuthCallbackPage />} />
              <Route element={<ProtectedRoute />}>
                <Route path="/" element={<Navigate to="/households" replace />} />
                <Route element={<AuthenticatedLayout />}>
                  <Route path="/households" element={<HouseholdsIndexPage />} />
                  <Route path="/households/:householdId" element={<DashboardPage />} />
                  <Route
                    path="/households/:householdId/settings"
                    element={<HouseholdSettingsPage />}
                  />
                  <Route path="/households/:householdId/assets" element={<AssetListPage />} />
                  <Route
                    path="/households/:householdId/assets/new"
                    element={<AssetCreateWizardPage />}
                  />
                  <Route
                    path="/households/:householdId/assets/:assetId"
                    element={<AssetDetailLayout />}
                  >
                    <Route index element={<Navigate to="engines" replace />} />
                    <Route path="engines" element={<EnginesSection />} />
                    <Route path="maintenance" element={<MaintenanceItemsPage />} />
                    <Route path="mileage-logs" element={<MileageLogsPage />} />
                    <Route path="engine-hours-logs" element={<EngineHoursLogsPage />} />
                    <Route path="fuel-logs" element={<FuelLogsPage />} />
                    <Route path="registrations" element={<RegistrationsSection />} />
                    <Route path="warranties" element={<WarrantiesSection />} />
                  </Route>
                  <Route
                    path="/households/:householdId/assets/:assetId/maintenance/:itemId"
                    element={<MaintenanceItemEditorPage />}
                  />
                  <Route
                    path="/households/:householdId/maintenance"
                    element={<MaintenanceKanbanPage />}
                  />
                  <Route path="/invites" element={<PendingInvitesPage />} />
                  <Route element={<PlatformAdminRoute />}>
                    <Route path="/admin" element={<AdminShellPage />}>
                      <Route index element={<Navigate to="templates" replace />} />
                      <Route path="templates" element={<AdminTemplatesPage />} />
                    </Route>
                  </Route>
                </Route>
              </Route>
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </ThemeProvider>
        </AuthProvider>
      </BrowserRouter>
      <Toaster />
    </QueryClientProvider>
  );
}

export default App;
