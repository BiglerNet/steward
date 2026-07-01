import { useParams } from "react-router";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { MembersPanel } from "@/components/households/MembersPanel";
import { RenameHouseholdForm } from "@/components/households/RenameHouseholdForm";
import { useHouseholds } from "@/hooks/useHouseholds";

export function HouseholdSettingsPage() {
  const { householdId } = useParams();
  const { data: households } = useHouseholds();
  const household = households?.find((item) => item.id === householdId);

  if (!household) {
    return null;
  }

  const canManage = household.userRole === "Owner" || household.userRole === "Contributor";

  return (
    <div className="max-w-2xl space-y-6">
      <h1 className="text-h1">Household settings</h1>
      <Card>
        <CardHeader>Household Name</CardHeader>
        <CardContent>
          <RenameHouseholdForm household={household} canEdit={canManage} />
        </CardContent>
      </Card>
      <Card>
        <CardHeader>Members</CardHeader>
        <CardContent>
          <MembersPanel householdId={household.id} canManage={canManage} />
        </CardContent>
      </Card>
    </div>
  );
}
