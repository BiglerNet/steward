import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  rectSortingStrategy,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Maximize2, X } from "lucide-react";
import type { DashboardSnapshot, WidgetResponse, WidgetSize } from "@/api/types";
import { DueSoonWidget, RecentActivityWidget, StatWidget } from "@/components/dashboard";

const SIZE_COLS: Record<WidgetSize, string> = {
  Small: "col-span-12 sm:col-span-6 lg:col-span-3",
  Wide: "col-span-12 sm:col-span-6",
  Full: "col-span-12",
};

const NEXT_SIZE: Record<WidgetSize, WidgetSize> = {
  Small: "Wide",
  Wide: "Full",
  Full: "Small",
};

export function reorderWidgets(
  widgets: WidgetResponse[],
  activeId: string,
  overId: string
): WidgetResponse[] | null {
  const oldIndex = widgets.findIndex((w) => w.id === activeId);
  const newIndex = widgets.findIndex((w) => w.id === overId);
  if (oldIndex === -1 || newIndex === -1) return null;

  return arrayMove(widgets, oldIndex, newIndex).map((w, index) => ({ ...w, position: index }));
}

interface WidgetGridProps {
  widgets: WidgetResponse[];
  snapshot: DashboardSnapshot;
  isEditing?: boolean;
  onReorder?: (widgets: WidgetResponse[]) => void;
  onResize?: (widgetId: string) => void;
  onRemove?: (widgetId: string) => void;
}

export function WidgetGrid({
  widgets,
  snapshot,
  isEditing = false,
  onReorder,
  onResize,
  onRemove,
}: WidgetGridProps) {
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  if (widgets.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        This dashboard has no widgets. Use &ldquo;Edit Dashboard&rdquo; to add some.
      </p>
    );
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const reordered = reorderWidgets(widgets, String(active.id), String(over.id));
    if (reordered) onReorder?.(reordered);
  }

  if (!isEditing) {
    return (
      <div className="grid grid-cols-12 gap-4">
        {widgets.map((widget) => (
          <div key={widget.id} data-widget-id={widget.id} className={SIZE_COLS[widget.widgetSize]}>
            {renderWidget(widget, snapshot)}
          </div>
        ))}
      </div>
    );
  }

  return (
    <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
      <SortableContext items={widgets.map((w) => w.id)} strategy={rectSortingStrategy}>
        <div className="grid grid-cols-12 gap-4">
          {widgets.map((widget) => (
            <EditableWidget
              key={widget.id}
              widget={widget}
              snapshot={snapshot}
              onResize={() => onResize?.(widget.id)}
              onRemove={() => onRemove?.(widget.id)}
            />
          ))}
        </div>
      </SortableContext>
    </DndContext>
  );
}

interface EditableWidgetProps {
  widget: WidgetResponse;
  snapshot: DashboardSnapshot;
  onResize: () => void;
  onRemove: () => void;
}

function EditableWidget({ widget, snapshot, onResize, onRemove }: EditableWidgetProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: widget.id,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      data-widget-id={widget.id}
      style={style}
      className={`${SIZE_COLS[widget.widgetSize]} ${isDragging ? "z-10 opacity-70" : ""}`}
    >
      <div className="mb-1 flex items-center justify-between gap-1">
        <button
          type="button"
          {...attributes}
          {...listeners}
          title="Drag to reorder"
          aria-label="Drag to reorder widget"
          className="cursor-grab touch-none rounded-md border border-border bg-background p-1 text-muted-foreground hover:text-foreground active:cursor-grabbing"
        >
          <GripVertical className="h-3.5 w-3.5" />
        </button>
        <div className="flex gap-1">
          <button
            type="button"
            onClick={onResize}
            title={`Resize (${widget.widgetSize} → ${NEXT_SIZE[widget.widgetSize]})`}
            aria-label="Resize widget"
            className="rounded-md border border-border bg-background p-1 text-muted-foreground hover:text-foreground"
          >
            <Maximize2 className="h-3.5 w-3.5" />
          </button>
          <button
            type="button"
            onClick={onRemove}
            title="Remove widget"
            aria-label="Remove widget"
            className="rounded-md border border-border bg-background p-1 text-muted-foreground hover:text-destructive"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
      {renderWidget(widget, snapshot)}
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
