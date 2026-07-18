import { useState } from "react";
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
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, MoreVertical } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import type { ChecklistItemResponse, ChecklistItemStatus, EngineResponse } from "@/api/types";

function reorderChecklist(
  items: ChecklistItemResponse[],
  activeId: string,
  overId: string
): string[] | null {
  const oldIndex = items.findIndex((i) => i.id === activeId);
  const newIndex = items.findIndex((i) => i.id === overId);
  if (oldIndex === -1 || newIndex === -1) return null;
  return arrayMove(items, oldIndex, newIndex).map((i) => i.id);
}

interface ChecklistSectionProps {
  items: ChecklistItemResponse[];
  engines: EngineResponse[];
  canEdit: boolean;
  onSetStatus: (checklistItemId: string, status: ChecklistItemStatus) => void;
  onMove: (checklistItemId: string, direction: "up" | "down") => void;
  onReorder: (checklistItemIds: string[]) => void;
  onAdd: (text: string) => void;
  onDelete: (checklistItemId: string) => void;
}

export function ChecklistSection({
  items,
  engines,
  canEdit,
  onSetStatus,
  onMove,
  onReorder,
  onAdd,
  onDelete,
}: ChecklistSectionProps) {
  const [newText, setNewText] = useState("");
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    if (!over || active.id === over.id) return;
    const reordered = reorderChecklist(items, String(active.id), String(over.id));
    if (reordered) onReorder(reordered);
  }

  function handleAdd(event: React.FormEvent) {
    event.preventDefault();
    if (!newText.trim()) return;
    onAdd(newText.trim());
    setNewText("");
  }

  return (
    <div className="space-y-3">
      <h3 className="text-h3">Checklist</h3>

      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground">No checklist items yet.</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={items.map((i) => i.id)} strategy={verticalListSortingStrategy}>
            <ul className="space-y-1">
              {items.map((item) => (
                <ChecklistRow
                  key={item.id}
                  item={item}
                  engineLabel={engines.find((e) => e.id === item.engineId)?.label}
                  canEdit={canEdit}
                  onSetStatus={onSetStatus}
                  onMove={onMove}
                  onDelete={onDelete}
                />
              ))}
            </ul>
          </SortableContext>
        </DndContext>
      )}

      {canEdit && (
        <form onSubmit={handleAdd} className="flex gap-2">
          <Input
            value={newText}
            onChange={(e) => setNewText(e.target.value)}
            placeholder="Add a checklist item…"
          />
          <Button type="submit" variant="outline" disabled={!newText.trim()}>
            Add
          </Button>
        </form>
      )}
    </div>
  );
}

interface ChecklistRowProps {
  item: ChecklistItemResponse;
  engineLabel: string | undefined;
  canEdit: boolean;
  onSetStatus: (checklistItemId: string, status: ChecklistItemStatus) => void;
  onMove: (checklistItemId: string, direction: "up" | "down") => void;
  onDelete: (checklistItemId: string) => void;
}

function ChecklistRow({ item, engineLabel, canEdit, onSetStatus, onMove, onDelete }: ChecklistRowProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: item.id,
    disabled: !canEdit,
  });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <li
      ref={setNodeRef}
      style={style}
      className={cn(
        "flex items-center gap-2 rounded-md border border-border bg-card px-2 py-1.5",
        isDragging && "z-10 opacity-70"
      )}
    >
      {canEdit && (
        <button
          type="button"
          {...attributes}
          {...listeners}
          title="Drag to reorder"
          aria-label="Drag to reorder checklist item"
          className="cursor-grab touch-none text-muted-foreground hover:text-foreground active:cursor-grabbing"
        >
          <GripVertical className="h-4 w-4" />
        </button>
      )}
      <Checkbox
        checked={item.status === "Done"}
        disabled={!canEdit}
        aria-label={`Mark "${item.text}" done`}
        onChange={(e) => onSetStatus(item.id, e.target.checked ? "Done" : "Open")}
      />
      <span
        className={cn(
          "flex-1 text-sm",
          item.status === "Done" && "text-muted-foreground line-through",
          item.status === "Skipped" && "text-muted-foreground italic"
        )}
      >
        {item.text}
        {engineLabel && <span className="ml-2 text-xs text-muted-foreground">({engineLabel})</span>}
        {item.status === "Skipped" && <span className="ml-2 text-xs">(Skipped)</span>}
      </span>
      {canEdit && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="Checklist item actions">
              <MoreVertical className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {item.status !== "Skipped" && (
              <DropdownMenuItem onSelect={() => onSetStatus(item.id, "Skipped")}>
                Mark skipped
              </DropdownMenuItem>
            )}
            {item.status !== "Open" && (
              <DropdownMenuItem onSelect={() => onSetStatus(item.id, "Open")}>Reopen</DropdownMenuItem>
            )}
            <DropdownMenuItem onSelect={() => onMove(item.id, "up")}>Move up</DropdownMenuItem>
            <DropdownMenuItem onSelect={() => onMove(item.id, "down")}>Move down</DropdownMenuItem>
            <DropdownMenuItem onSelect={() => onDelete(item.id)} className="text-destructive">
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </li>
  );
}
