import {
  Anchor,
  BatteryCharging,
  Bike,
  Box,
  Car,
  CarFront,
  CarTaxiFront,
  Caravan,
  Cog,
  Container,
  Mountain,
  Package,
  Sailboat,
  Ship,
  Snowflake,
  SprayCan,
  Tractor,
  Truck,
  Van,
  Zap,
  type LucideIcon,
} from "lucide-react";
import type { AssetGroup } from "@/api/types";
import { cn } from "@/lib/utils";

/// Maps the registry's `icon` (kebab-case lucide name) to a component. Names with no entry
/// here fall back to a neutral icon rather than a letter, blank, or error.
const ICON_MAP: Record<string, LucideIcon> = {
  anchor: Anchor,
  "battery-charging": BatteryCharging,
  bike: Bike,
  car: Car,
  "car-front": CarFront,
  "car-taxi-front": CarTaxiFront,
  caravan: Caravan,
  cog: Cog,
  container: Container,
  mountain: Mountain,
  package: Package,
  sailboat: Sailboat,
  ship: Ship,
  snowflake: Snowflake,
  "spray-can": SprayCan,
  tractor: Tractor,
  truck: Truck,
  van: Van,
  zap: Zap,
};

const FALLBACK_ICON: LucideIcon = Box;

/// Chip tint is per registry group, not per category — five deliberate light/dark pairs
/// defined as CSS variables in index.css, never colors from the API.
const GROUP_CHIP_CLASSES: Record<AssetGroup, string> = {
  Road: "bg-asset-chip-road-bg text-asset-chip-road-fg",
  Powersport: "bg-asset-chip-powersport-bg text-asset-chip-powersport-fg",
  Water: "bg-asset-chip-water-bg text-asset-chip-water-fg",
  Trailer: "bg-asset-chip-trailer-bg text-asset-chip-trailer-fg",
  Equipment: "bg-asset-chip-equipment-bg text-asset-chip-equipment-fg",
};

type AssetTypeIconSize = "sm" | "md";

const SIZE_CLASSES: Record<AssetTypeIconSize, { chip: string; icon: string }> = {
  sm: { chip: "h-8 w-8 rounded-lg", icon: "h-4 w-4" },
  md: { chip: "h-11 w-11 rounded-[10px]", icon: "h-5 w-5" },
};

export interface AssetTypeIconProps {
  icon: string;
  group: AssetGroup;
  size?: AssetTypeIconSize;
  className?: string;
}

export function AssetTypeIcon({ icon, group, size = "md", className }: AssetTypeIconProps) {
  const Icon = ICON_MAP[icon] ?? FALLBACK_ICON;
  const sizeClasses = SIZE_CLASSES[size];

  return (
    <span
      aria-hidden="true"
      className={cn(
        "flex shrink-0 items-center justify-center",
        sizeClasses.chip,
        GROUP_CHIP_CLASSES[group],
        className
      )}
    >
      <Icon className={sizeClasses.icon} />
    </span>
  );
}
