import { Card, CardContent, CardHeader } from "@/components/ui/card";

interface StatWidgetProps {
  label: string;
  value: string | number;
  subLabel?: string;
}

export function StatWidget({ label, value, subLabel }: StatWidgetProps) {
  return (
    <Card className="h-full">
      <CardHeader>{label}</CardHeader>
      <CardContent className="flex flex-col gap-1 pt-4">
        <span className="text-3xl font-bold tabular-nums">{value}</span>
        {subLabel && <span className="text-sm text-muted-foreground">{subLabel}</span>}
      </CardContent>
    </Card>
  );
}
