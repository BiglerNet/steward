import { Navigate } from "react-router";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { CreateHouseholdDialog } from "@/components/households/CreateHouseholdDialog";
import { useHouseholds } from "@/hooks/useHouseholds";
import { readLastHouseholdId } from "@/lib/session";

export function HouseholdsIndexPage() {
  const { data: households, isLoading } = useHouseholds();

  if (isLoading) {
    return null;
  }

  if (households && households.length > 0) {
    const lastHouseholdId = readLastHouseholdId();
    const target = households.find((household) => household.id === lastHouseholdId) ?? households[0];
    return <Navigate to={`/households/${target.id}`} replace />;
  }

  return (
    <div className="mx-auto max-w-md py-16 text-center">
      <h1 className="text-h1">Create your first household</h1>
      <p className="mx-auto mt-2 max-w-sm text-body text-muted-foreground">
        Households are how you organize and share assets with the people you live with.
      </p>
      <Card className="mt-8 shadow-[0_4px_24px_rgba(0,0,0,0.04)]">
        <CardContent className="flex flex-col items-center gap-4 py-8">
          <CreateHouseholdDialog trigger={<Button>Create household</Button>} />
        </CardContent>
      </Card>
    </div>
  );
}
