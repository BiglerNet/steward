import type { DueItem } from "@/api/types";
import { Card, CardContent, CardHeader } from "@/components/ui/card";

const URGENCY_COLOR: Record<string, string> = {
  Overdue: "bg-red-500",
  DueSoon: "bg-amber-500",
  Upcoming: "bg-green-500",
};

interface DueSoonWidgetProps {
  items: DueItem[];
}

export function DueSoonWidget({ items }: DueSoonWidgetProps) {
  return (
    <Card className="h-full">
      <CardHeader>Due Soon</CardHeader>
      <CardContent className="pt-4">
        {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">Nothing due soon.</p>
        ) : (
          <ul className="space-y-2">
            {items.map((item, i) => (
              <li key={i} className="flex items-center gap-3 text-sm">
                <span
                  className={`h-2.5 w-2.5 shrink-0 rounded-full ${URGENCY_COLOR[item.urgency] ?? "bg-muted"}`}
                  aria-label={item.urgency}
                />
                <span className="flex-1 font-medium">{item.assetName}</span>
                <span className="text-muted-foreground">{item.recordType}</span>
                <span className="tabular-nums text-muted-foreground">{item.expiresOn}</span>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
