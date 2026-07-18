import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TemplateEditor } from "@/components/templates/TemplateEditor";
import { useAdminTemplateMutations } from "@/hooks/useAdminTemplateMutations";
import { usePlatformTemplates } from "@/hooks/useTemplates";

export function AdminTemplatesPage() {
  const { data: templates } = usePlatformTemplates();
  const mutations = useAdminTemplateMutations();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const selected = templates?.find((t) => t.id === selectedId) ?? null;
  const filteredTemplates = useMemo(
    () => (templates ?? []).filter((t) => t.title.toLowerCase().includes(search.toLowerCase())),
    [templates, search]
  );

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
      <div className="space-y-2 lg:col-span-1">
        <div className="flex items-center justify-between">
          <h2 className="text-h3">Platform templates</h2>
          <CreateTemplateDialog
            onCreate={(title) =>
              mutations.createTemplate.mutate(
                { title },
                {
                  onSuccess: (created) => setSelectedId(created.id),
                  onError: () => toast.error("Couldn't create this template."),
                }
              )
            }
          />
        </div>
        {templates && templates.length > 0 && (
          <Input placeholder="Search…" value={search} onChange={(e) => setSearch(e.target.value)} />
        )}
        {!templates || templates.length === 0 ? (
          <p className="text-sm text-muted-foreground">No platform templates yet.</p>
        ) : filteredTemplates.length === 0 ? (
          <p className="text-sm text-muted-foreground">No templates match your search.</p>
        ) : (
          <ul className="space-y-1">
            {filteredTemplates.map((template) => (
              <li key={template.id}>
                <button
                  type="button"
                  onClick={() => setSelectedId(template.id)}
                  className={
                    template.id === selectedId
                      ? "w-full rounded-md bg-accent px-3 py-2 text-left text-sm"
                      : "w-full rounded-md px-3 py-2 text-left text-sm hover:bg-accent"
                  }
                >
                  {template.title}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="lg:col-span-2">
        {selected ? (
          <TemplateEditor
            template={selected}
            canEdit
            mutations={mutations}
            onDeleted={() => setSelectedId(null)}
          />
        ) : (
          <p className="text-sm text-muted-foreground">Select a template to view or edit it.</p>
        )}
      </div>
    </div>
  );
}

function CreateTemplateDialog({ onCreate }: { onCreate: (title: string) => void }) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState("");

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    if (!title.trim()) return;
    onCreate(title.trim());
    setTitle("");
    setOpen(false);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm">New template</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>New platform template</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="new-admin-template-title">Title</Label>
            <Input
              id="new-admin-template-title"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!title.trim()}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
