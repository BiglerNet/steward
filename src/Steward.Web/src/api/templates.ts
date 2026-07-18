import { apiClient } from "@/api/client";
import type {
  AssetCategory,
  CreateTemplateRequest,
  CreateTemplateStepRequest,
  DuplicateTemplateRequest,
  PatchTemplateRequest,
  PatchTemplateStepRequest,
  TemplateResponse,
  TemplateStepResponse,
} from "@/api/types";

export async function listHouseholdTemplates(
  householdId: string,
  assetCategory?: AssetCategory
): Promise<TemplateResponse[]> {
  const { data } = await apiClient.get<TemplateResponse[]>(
    `/api/households/${householdId}/templates`,
    { params: assetCategory ? { assetCategory } : undefined }
  );
  return data;
}

export async function createHouseholdTemplate(
  householdId: string,
  request: CreateTemplateRequest
): Promise<TemplateResponse> {
  const { data } = await apiClient.post<TemplateResponse>(
    `/api/households/${householdId}/templates`,
    request
  );
  return data;
}

export async function patchHouseholdTemplate(
  householdId: string,
  templateId: string,
  request: PatchTemplateRequest
): Promise<TemplateResponse> {
  const { data } = await apiClient.patch<TemplateResponse>(
    `/api/households/${householdId}/templates/${templateId}`,
    request
  );
  return data;
}

export async function deleteHouseholdTemplate(householdId: string, templateId: string): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/templates/${templateId}`);
}

export async function duplicateTemplate(
  householdId: string,
  request: DuplicateTemplateRequest
): Promise<TemplateResponse> {
  const { data } = await apiClient.post<TemplateResponse>(
    `/api/households/${householdId}/templates/duplicate`,
    request
  );
  return data;
}

export async function createHouseholdTemplateStep(
  householdId: string,
  templateId: string,
  request: CreateTemplateStepRequest
): Promise<TemplateStepResponse> {
  const { data } = await apiClient.post<TemplateStepResponse>(
    `/api/households/${householdId}/templates/${templateId}/steps`,
    request
  );
  return data;
}

export async function patchHouseholdTemplateStep(
  householdId: string,
  templateId: string,
  stepId: string,
  request: PatchTemplateStepRequest
): Promise<TemplateStepResponse> {
  const { data } = await apiClient.patch<TemplateStepResponse>(
    `/api/households/${householdId}/templates/${templateId}/steps/${stepId}`,
    request
  );
  return data;
}

export async function deleteHouseholdTemplateStep(
  householdId: string,
  templateId: string,
  stepId: string
): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/templates/${templateId}/steps/${stepId}`);
}

export async function reorderHouseholdTemplateSteps(
  householdId: string,
  templateId: string,
  stepIds: string[]
): Promise<TemplateStepResponse[]> {
  const { data } = await apiClient.put<TemplateStepResponse[]>(
    `/api/households/${householdId}/templates/${templateId}/steps/reorder`,
    { stepIds }
  );
  return data;
}

export async function listPlatformTemplates(assetCategory?: AssetCategory): Promise<TemplateResponse[]> {
  const { data } = await apiClient.get<TemplateResponse[]>(`/api/templates/platform`, {
    params: assetCategory ? { assetCategory } : undefined,
  });
  return data;
}

export async function createAdminTemplate(request: CreateTemplateRequest): Promise<TemplateResponse> {
  const { data } = await apiClient.post<TemplateResponse>(`/api/admin/templates`, request);
  return data;
}

export async function patchAdminTemplate(
  templateId: string,
  request: PatchTemplateRequest
): Promise<TemplateResponse> {
  const { data } = await apiClient.patch<TemplateResponse>(
    `/api/admin/templates/${templateId}`,
    request
  );
  return data;
}

export async function deleteAdminTemplate(templateId: string): Promise<void> {
  await apiClient.delete(`/api/admin/templates/${templateId}`);
}

export async function createAdminTemplateStep(
  templateId: string,
  request: CreateTemplateStepRequest
): Promise<TemplateStepResponse> {
  const { data } = await apiClient.post<TemplateStepResponse>(
    `/api/admin/templates/${templateId}/steps`,
    request
  );
  return data;
}

export async function patchAdminTemplateStep(
  templateId: string,
  stepId: string,
  request: PatchTemplateStepRequest
): Promise<TemplateStepResponse> {
  const { data } = await apiClient.patch<TemplateStepResponse>(
    `/api/admin/templates/${templateId}/steps/${stepId}`,
    request
  );
  return data;
}

export async function deleteAdminTemplateStep(templateId: string, stepId: string): Promise<void> {
  await apiClient.delete(`/api/admin/templates/${templateId}/steps/${stepId}`);
}

export async function reorderAdminTemplateSteps(
  templateId: string,
  stepIds: string[]
): Promise<TemplateStepResponse[]> {
  const { data } = await apiClient.put<TemplateStepResponse[]>(
    `/api/admin/templates/${templateId}/steps/reorder`,
    { stepIds }
  );
  return data;
}
