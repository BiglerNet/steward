import type { DashboardSnapshot, WidgetResponse, WidgetSize } from "@/api/types";
import { DueSoonWidget, RecentActivityWidget, StatWidget } from "@/components/dashboard";

const SIZE_COLS: Record<WidgetSize, string> = {
  Small: "col-span-12 sm:col-span-6 lg:col-span-3",
  Wide: "col-span-12 sm:col-span-6",
  Full: "col-span-12",
};

interface WidgetGridProps {
  widgets: WidgetResponse[];
  snapshot: DashboardSnapshot;
}

export function WidgetGrid({ widgets, snapshot }: WidgetGridProps) {
  if (widgets.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        This dashboard has no widgets. Use &ldquo;Edit Dashboard&rdquo; to add some.
      </p>
    );
  }

  return (
    <div className="grid grid-cols-12 gap-4">
      {widgets.map((widget) => (
        <div key={widget.id} className={SIZE_COLS[widget.widgetSize]}>
          {renderWidget(widget, snapshot)}
        </div>
      ))}
    </div>
  );
}

function renderWidget(widget: WidgetResponse, snapshot: DashboardSnapshot) {
  switch (widget.widgetType) {
    case "AssetCount": {
      const data = snapshot.AssetCount;
      return <StatWidget label="Assets" value={data?.count ?? "—"} />;
    }
    case "CylinderIndex": {
      const data = snapshot.CylinderIndex;
      return (
        <StatWidget
          label="Cylinder Index"
          value={data?.totalCylinders ?? "—"}
          subLabel={data ? `${data.engineCount} active ICE engine${data.engineCount !== 1 ? "s" : ""}` : undefined}
        />
      );
    }
    case "TotalDisplacement": {
      const data = snapshot.TotalDisplacement;
      return (
        <StatWidget
          label="Total Displacement"
          value={data ? `${data.totalCc.toLocaleString()} cc` : "—"}
          subLabel={data ? `${data.engineCount} active engine${data.engineCount !== 1 ? "s" : ""}` : undefined}
        />
      );
    }
    case "TotalHorsepower": {
      const data = snapshot.TotalHorsepower;
      return (
        <StatWidget
          label="Total Horsepower"
          value={data ? `${data.totalHp} HP` : "—"}
          subLabel={data ? `${data.engineCount} active engine${data.engineCount !== 1 ? "s" : ""}` : undefined}
        />
      );
    }
    case "TotalTorque": {
      const data = snapshot.TotalTorque;
      return (
        <StatWidget
          label="Total Torque"
          value={data ? `${Math.round(data.totalNm * 0.7376)} ft-lbs` : "—"}
          subLabel={data ? `${data.engineCount} active engine${data.engineCount !== 1 ? "s" : ""}` : undefined}
        />
      );
    }
    case "DueSoon": {
      const data = snapshot.DueSoon;
      return <DueSoonWidget items={data?.items ?? []} />;
    }
    case "RecentActivity": {
      const data = snapshot.RecentActivity;
      return <RecentActivityWidget items={data?.items ?? []} />;
    }
    case "FuelCostYtd": {
      const data = snapshot.FuelCostYtd;
      const cost = data != null
        ? new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(data.totalCost)
        : "—";
      return <StatWidget label="Fuel Cost YTD" value={cost} subLabel={data ? `${data.logCount} fill-ups` : undefined} />;
    }
    case "MileageMtd": {
      const data = snapshot.MileageMtd;
      return (
        <StatWidget
          label="Mileage MTD"
          value={data ? `${data.totalMiles.toLocaleString()} mi` : "—"}
          subLabel={data ? `${data.logCount} log${data.logCount !== 1 ? "s" : ""}` : undefined}
        />
      );
    }
    default:
      return null;
  }
}
