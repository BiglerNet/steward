import { useMutation, useQueryClient } from "@tanstack/react-query";
import type {
  CreateTemplateRequest,
  CreateTemplateStepRequest,
  PatchTemplateRequest,
  PatchTemplateStepRequest,
  TemplateResponse,
  TemplateStepResponse,
} from "@/api/types";

export interface TemplateEditorApi {
  createTemplate: (request: CreateTemplateRequest) => Promise<TemplateResponse>;
  patchTemplate: (templateId: string, request: PatchTemplateRequest) => Promise<TemplateResponse>;
  deleteTemplate: (templateId: string) => Promise<void>;
  createStep: (templateId: string, request: CreateTemplateStepRequest) => Promise<TemplateStepResponse>;
  patchStep: (
    templateId: string,
    stepId: string,
    request: PatchTemplateStepRequest
  ) => Promise<TemplateStepResponse>;
  deleteStep: (templateId: string, stepId: string) => Promise<void>;
  reorderSteps: (templateId: string, stepIds: string[]) => Promise<TemplateStepResponse[]>;
}

/**
 * Shared CRUD + step-management mutations for a template editor screen. Parameterized by API
 * functions and a cache key so the household templates screen and the admin platform-templates
 * screen (identical shapes, different endpoints) can reuse one implementation.
 */
export function useTemplateEditorMutations(api: TemplateEditorApi, queryKey: unknown[]) {
  const queryClient = useQueryClient();

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
  }

  const createTemplate = useMutation({
    mutationFn: (request: CreateTemplateRequest) => api.createTemplate(request),
    onSuccess: invalidate,
  });

  const patchTemplate = useMutation({
    mutationFn: ({ templateId, request }: { templateId: string; request: PatchTemplateRequest }) =>
      api.patchTemplate(templateId, request),
    onSuccess: invalidate,
  });

  const deleteTemplate = useMutation({
    mutationFn: (templateId: string) => api.deleteTemplate(templateId),
    onSuccess: invalidate,
  });

  const createStep = useMutation({
    mutationFn: ({ templateId, request }: { templateId: string; request: CreateTemplateStepRequest }) =>
      api.createStep(templateId, request),
    onSuccess: invalidate,
  });

  const patchStep = useMutation({
    mutationFn: ({
      templateId,
      stepId,
      request,
    }: {
      templateId: string;
      stepId: string;
      request: PatchTemplateStepRequest;
    }) => api.patchStep(templateId, stepId, request),
    onSuccess: invalidate,
  });

  const deleteStep = useMutation({
    mutationFn: ({ templateId, stepId }: { templateId: string; stepId: string }) =>
      api.deleteStep(templateId, stepId),
    onSuccess: invalidate,
  });

  const reorderSteps = useMutation({
    mutationFn: ({ templateId, stepIds }: { templateId: string; stepIds: string[] }) =>
      api.reorderSteps(templateId, stepIds),
    onSuccess: invalidate,
  });

  return { createTemplate, patchTemplate, deleteTemplate, createStep, patchStep, deleteStep, reorderSteps };
}
