import { useParams } from "react-router";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { HouseholdLocationForm } from "@/components/households/HouseholdLocationForm";
import { MembersPanel } from "@/components/households/MembersPanel";
import { RenameHouseholdForm } from "@/components/households/RenameHouseholdForm";
import { StorageUsageSummary } from "@/components/households/StorageUsageSummary";
import { HouseholdTemplatesSection } from "@/pages/templates/HouseholdTemplatesPage";
import { useHouseholds } from "@/hooks/useHouseholds";

export function HouseholdSettingsPage() {
  const { householdId } = useParams();
  const { data: households } = useHouseholds();
  const household = households?.find((item) => item.id === householdId);

  if (!household) {
    return null;
  }

  const canManage = household.userRole === "Owner" || household.userRole === "Contributor";
  const isOwner = household.userRole === "Owner";

  return (
    <div className="max-w-4xl space-y-6">
      <h1 className="text-h1">Household settings</h1>
      <Tabs defaultValue="general">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="maintenance-templates">Maintenance Templates</TabsTrigger>
        </TabsList>
        <TabsContent value="general" className="max-w-2xl space-y-6">
          <Card>
            <CardHeader>Household Name</CardHeader>
            <CardContent>
              <RenameHouseholdForm household={household} canEdit={canManage} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>Location</CardHeader>
            <CardContent>
              <HouseholdLocationForm household={household} canEdit={isOwner} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>Members</CardHeader>
            <CardContent>
              <MembersPanel householdId={household.id} canManage={canManage} />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>Storage</CardHeader>
            <CardContent>
              <StorageUsageSummary
                usedBytes={household.storageUsedBytes}
                quotaBytes={household.storageQuotaBytes}
              />
            </CardContent>
          </Card>
        </TabsContent>
        <TabsContent value="maintenance-templates">
          <HouseholdTemplatesSection householdId={household.id} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
