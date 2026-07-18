import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createHouseholdTemplate,
  createHouseholdTemplateStep,
  deleteHouseholdTemplate,
  deleteHouseholdTemplateStep,
  duplicateTemplate,
  patchHouseholdTemplate,
  patchHouseholdTemplateStep,
  reorderHouseholdTemplateSteps,
} from "@/api/templates";
import { useTemplateEditorMutations } from "@/hooks/useTemplateEditorMutations";

export function useHouseholdTemplateMutations(householdId: string) {
  return useTemplateEditorMutations(
    {
      createTemplate: (request) => createHouseholdTemplate(householdId, request),
      patchTemplate: (templateId, request) => patchHouseholdTemplate(householdId, templateId, request),
      deleteTemplate: (templateId) => deleteHouseholdTemplate(householdId, templateId),
      createStep: (templateId, request) => createHouseholdTemplateStep(householdId, templateId, request),
      patchStep: (templateId, stepId, request) =>
        patchHouseholdTemplateStep(householdId, templateId, stepId, request),
      deleteStep: (templateId, stepId) => deleteHouseholdTemplateStep(householdId, templateId, stepId),
      reorderSteps: (templateId, stepIds) => reorderHouseholdTemplateSteps(householdId, templateId, stepIds),
    },
    ["households", householdId, "templates"]
  );
}

export function useDuplicateTemplate(householdId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (platformTemplateId: string) => duplicateTemplate(householdId, { platformTemplateId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["households", householdId, "templates"] });
    },
  });
}
