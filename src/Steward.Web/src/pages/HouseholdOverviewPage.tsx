import { Link, useParams } from "react-router";
import { Card, CardContent } from "@/components/ui/card";
import { useHouseholds } from "@/hooks/useHouseholds";

export function HouseholdOverviewPage() {
  const { householdId } = useParams();
  const { data: households } = useHouseholds();
  const household = households?.find((item) => item.id === householdId);

  return (
    <div className="space-y-5">
      <h1 className="text-h1">{household?.name ?? "Household"}</h1>
      <Card>
        <CardContent className="py-5">
          <Link
            to={`/households/${householdId}/assets`}
            className="text-body font-medium text-primary underline-offset-4 hover:underline"
          >
            View assets
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
