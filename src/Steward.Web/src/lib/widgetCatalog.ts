import type { WidgetSize, WidgetType } from "@/api/types";

export interface WidgetCatalogEntry {
  type: WidgetType;
  label: string;
  defaultSize: WidgetSize;
}

export const WIDGET_CATALOG: WidgetCatalogEntry[] = [
  { type: "AssetCount", label: "Asset Count", defaultSize: "Small" },
  { type: "CylinderIndex", label: "Cylinder Index", defaultSize: "Small" },
  { type: "TotalDisplacement", label: "Total Displacement", defaultSize: "Small" },
  { type: "TotalHorsepower", label: "Total Horsepower", defaultSize: "Small" },
  { type: "TotalTorque", label: "Total Torque", defaultSize: "Small" },
  { type: "DueSoon", label: "Due Soon", defaultSize: "Full" },
  { type: "RecentActivity", label: "Recent Activity", defaultSize: "Full" },
  { type: "FuelCostYtd", label: "Fuel Cost YTD", defaultSize: "Wide" },
  { type: "MileageMtd", label: "Mileage MTD", defaultSize: "Wide" },
];
