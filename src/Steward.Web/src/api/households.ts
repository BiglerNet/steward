import { apiClient } from "@/api/client";
import type {
  CreateHouseholdRequest,
  HouseholdMembersResponse,
  HouseholdResponse,
  InviteMemberRequest,
  UpdateHouseholdRequest,
} from "@/api/types";

export async function listHouseholds(): Promise<HouseholdResponse[]> {
  const { data } = await apiClient.get<HouseholdResponse[]>("/api/households");
  return data;
}

export async function createHousehold(request: CreateHouseholdRequest): Promise<HouseholdResponse> {
  const { data } = await apiClient.post<HouseholdResponse>("/api/households", request);
  return data;
}

export async function getHousehold(id: string): Promise<HouseholdResponse> {
  const { data } = await apiClient.get<HouseholdResponse>(`/api/households/${id}`);
  return data;
}

export async function updateHousehold(
  id: string,
  request: UpdateHouseholdRequest
): Promise<HouseholdResponse> {
  const { data } = await apiClient.put<HouseholdResponse>(`/api/households/${id}`, request);
  return data;
}

export async function listMembers(householdId: string): Promise<HouseholdMembersResponse> {
  const { data } = await apiClient.get<HouseholdMembersResponse>(
    `/api/households/${householdId}/members`
  );
  return data;
}

export async function inviteMember(
  householdId: string,
  request: InviteMemberRequest
): Promise<void> {
  await apiClient.post(`/api/households/${householdId}/members/invite`, request);
}

export async function revokeInvitation(householdId: string, code: string): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/invitations/${encodeURIComponent(code)}`);
}

export async function removeMember(householdId: string, userId: string): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/members/${userId}`);
}
