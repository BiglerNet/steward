import {
  createAdminTemplate,
  createAdminTemplateStep,
  deleteAdminTemplate,
  deleteAdminTemplateStep,
  patchAdminTemplate,
  patchAdminTemplateStep,
  reorderAdminTemplateSteps,
} from "@/api/templates";
import { useTemplateEditorMutations } from "@/hooks/useTemplateEditorMutations";

export function useAdminTemplateMutations() {
  return useTemplateEditorMutations(
    {
      createTemplate: (request) => createAdminTemplate(request),
      patchTemplate: (templateId, request) => patchAdminTemplate(templateId, request),
      deleteTemplate: (templateId) => deleteAdminTemplate(templateId),
      createStep: (templateId, request) => createAdminTemplateStep(templateId, request),
      patchStep: (templateId, stepId, request) => patchAdminTemplateStep(templateId, stepId, request),
      deleteStep: (templateId, stepId) => deleteAdminTemplateStep(templateId, stepId),
      reorderSteps: (templateId, stepIds) => reorderAdminTemplateSteps(templateId, stepIds),
    },
    ["templates", "platform"]
  );
}
