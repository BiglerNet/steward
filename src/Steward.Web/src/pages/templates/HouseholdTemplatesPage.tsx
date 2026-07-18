import { useMemo, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { TemplateEditor } from "@/components/templates/TemplateEditor";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { useDuplicateTemplate, useHouseholdTemplateMutations } from "@/hooks/useHouseholdTemplateMutations";
import { useHouseholdTemplates, usePlatformTemplates } from "@/hooks/useTemplates";
import { useHouseholdRole } from "@/lib/permissions";

interface HouseholdTemplatesSectionProps {
  householdId: string;
}

export function HouseholdTemplatesSection({ householdId }: HouseholdTemplatesSectionProps) {
  const { canEdit } = useHouseholdRole();
  const { data: templates } = useHouseholdTemplates(householdId);
  const { data: platformTemplates } = usePlatformTemplates();
  const mutations = useHouseholdTemplateMutations(householdId);
  const duplicateMutation = useDuplicateTemplate(householdId);

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [householdSearch, setHouseholdSearch] = useState("");
  const [platformSearch, setPlatformSearch] = useState("");
  const selected = templates?.find((t) => t.id === selectedId) ?? null;

  const filteredHouseholdTemplates = useMemo(
    () => (templates ?? []).filter((t) => t.title.toLowerCase().includes(householdSearch.toLowerCase())),
    [templates, householdSearch]
  );
  const filteredPlatformTemplates = useMemo(
    () => (platformTemplates ?? []).filter((t) => t.title.toLowerCase().includes(platformSearch.toLowerCase())),
    [platformTemplates, platformSearch]
  );

  function handleDuplicate(platformTemplateId: string) {
    duplicateMutation.mutate(platformTemplateId, {
      onSuccess: (created) => setSelectedId(created.id),
      onError: () => toast.error("Couldn't duplicate this template."),
    });
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Reusable maintenance checklists you can apply when logging work on an asset.
        </p>
        {canEdit && (
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
        )}
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-1">
          <div>
            <h2 className="text-h3">Household templates</h2>
            {templates && templates.length > 0 && (
              <Input
                className="mt-2"
                placeholder="Search…"
                value={householdSearch}
                onChange={(e) => setHouseholdSearch(e.target.value)}
              />
            )}
            {!templates || templates.length === 0 ? (
              <p className="mt-2 text-sm text-muted-foreground">No templates yet.</p>
            ) : filteredHouseholdTemplates.length === 0 ? (
              <p className="mt-2 text-sm text-muted-foreground">No templates match your search.</p>
            ) : (
              <ul className="mt-2 space-y-1">
                {filteredHouseholdTemplates.map((template) => (
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

          <div>
            <h2 className="text-h3">Platform library</h2>
            {platformTemplates && platformTemplates.length > 0 && (
              <Input
                className="mt-2"
                placeholder="Search…"
                value={platformSearch}
                onChange={(e) => setPlatformSearch(e.target.value)}
              />
            )}
            {!platformTemplates || platformTemplates.length === 0 ? (
              <p className="mt-2 text-sm text-muted-foreground">No platform templates.</p>
            ) : filteredPlatformTemplates.length === 0 ? (
              <p className="mt-2 text-sm text-muted-foreground">No templates match your search.</p>
            ) : (
              <ul className="mt-2 space-y-1">
                {filteredPlatformTemplates.map((template) => (
                  <li key={template.id} className="flex items-center justify-between gap-2 px-3 py-2 text-sm">
                    <span>{template.title}</span>
                    {canEdit && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={duplicateMutation.isPending}
                        onClick={() => handleDuplicate(template.id)}
                        title="Platform templates are read-only — copy it to edit your own version"
                      >
                        Copy to my library to modify
                      </Button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="lg:col-span-2">
          {selected ? (
            <TemplateEditor
              template={selected}
              canEdit={canEdit}
              mutations={mutations}
              onDeleted={() => setSelectedId(null)}
            />
          ) : (
            <p className="text-sm text-muted-foreground">Select a template to view or edit it.</p>
          )}
        </div>
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
          <DialogTitle>New template</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="new-template-title">Title</Label>
            <Input id="new-template-title" value={title} onChange={(e) => setTitle(e.target.value)} autoFocus />
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
