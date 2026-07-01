import type { DashboardSummaryResponse } from "@/api/types";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { writeLastDashboardId } from "@/lib/dashboardStorage";

interface DashboardSelectorProps {
  dashboards: DashboardSummaryResponse[];
  activeDashboardId: string;
  householdId: string;
  onSelect: (id: string) => void;
}

export function DashboardSelector({
  dashboards,
  activeDashboardId,
  householdId,
  onSelect,
}: DashboardSelectorProps) {
  function handleValueChange(id: string) {
    writeLastDashboardId(householdId, id);
    onSelect(id);
  }

  return (
    <Tabs value={activeDashboardId} onValueChange={handleValueChange}>
      <TabsList>
        {dashboards.map((d) => (
          <TabsTrigger key={d.id} value={d.id}>
            {d.name}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  );
}
