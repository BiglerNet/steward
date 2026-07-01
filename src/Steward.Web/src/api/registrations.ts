import { apiClient } from "@/api/client";
import type {
  CreateRegistrationRequest,
  RegistrationResponse,
  UpdateRegistrationRequest,
} from "@/api/types";

export async function listRegistrations(
  householdId: string,
  assetId: string
): Promise<RegistrationResponse[]> {
  const { data } = await apiClient.get<RegistrationResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/registrations`
  );
  return data;
}

export async function createRegistration(
  householdId: string,
  assetId: string,
  request: CreateRegistrationRequest
): Promise<RegistrationResponse> {
  const { data } = await apiClient.post<RegistrationResponse>(
    `/api/households/${householdId}/assets/${assetId}/registrations`,
    request
  );
  return data;
}

export async function updateRegistration(
  householdId: string,
  assetId: string,
  registrationId: string,
  request: UpdateRegistrationRequest
): Promise<RegistrationResponse> {
  const { data } = await apiClient.put<RegistrationResponse>(
    `/api/households/${householdId}/assets/${assetId}/registrations/${registrationId}`,
    request
  );
  return data;
}

export async function deleteRegistration(
  householdId: string,
  assetId: string,
  registrationId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/registrations/${registrationId}`
  );
}
