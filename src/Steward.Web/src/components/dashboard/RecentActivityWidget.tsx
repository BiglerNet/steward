import type { ActivityItem } from "@/api/types";
import { Card, CardContent, CardHeader } from "@/components/ui/card";

interface RecentActivityWidgetProps {
  items: ActivityItem[];
}

function formatCost(cost: number | null): string {
  if (cost == null) return "";
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(cost);
}

export function RecentActivityWidget({ items }: RecentActivityWidgetProps) {
  return (
    <Card className="h-full">
      <CardHeader>Recent Activity</CardHeader>
      <CardContent className="pt-4">
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">No recent activity.</p>
        ) : (
          <ul className="space-y-3">
            {items.map((item, i) => (
              <li key={i} className="flex items-start justify-between gap-4 text-sm">
                <div className="flex flex-col gap-0.5">
                  <span className="font-medium">{item.assetName}</span>
                  <span className="text-muted-foreground">{item.description}</span>
                </div>
                <div className="flex flex-col items-end gap-0.5 shrink-0 text-muted-foreground tabular-nums">
                  <span>{item.performedOn}</span>
                  {item.cost != null && <span>{formatCost(item.cost)}</span>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
