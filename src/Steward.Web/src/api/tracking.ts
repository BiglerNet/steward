import { apiClient } from "@/api/client";
import type {
  CreateEngineHoursLogRequest,
  CreateFuelLogRequest,
  CreateMileageLogRequest,
  CreateServiceRecordRequest,
  EngineHoursLogResponse,
  FuelLogResponse,
  MileageLogResponse,
  ServiceRecordResponse,
  UpdateEngineHoursLogRequest,
  UpdateFuelLogRequest,
  UpdateMileageLogRequest,
  UpdateServiceRecordRequest,
} from "@/api/types";

export async function listServiceRecords(
  householdId: string,
  assetId: string
): Promise<ServiceRecordResponse[]> {
  const { data } = await apiClient.get<ServiceRecordResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/service-records`
  );
  return data;
}

export async function createServiceRecord(
  householdId: string,
  assetId: string,
  request: CreateServiceRecordRequest
): Promise<ServiceRecordResponse> {
  const { data } = await apiClient.post<ServiceRecordResponse>(
    `/api/households/${householdId}/assets/${assetId}/service-records`,
    request
  );
  return data;
}

export async function updateServiceRecord(
  householdId: string,
  assetId: string,
  serviceRecordId: string,
  request: UpdateServiceRecordRequest
): Promise<ServiceRecordResponse> {
  const { data } = await apiClient.put<ServiceRecordResponse>(
    `/api/households/${householdId}/assets/${assetId}/service-records/${serviceRecordId}`,
    request
  );
  return data;
}

export async function deleteServiceRecord(
  householdId: string,
  assetId: string,
  serviceRecordId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/service-records/${serviceRecordId}`
  );
}

export async function listMileageLogs(
  householdId: string,
  assetId: string
): Promise<MileageLogResponse[]> {
  const { data } = await apiClient.get<MileageLogResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/mileage-logs`
  );
  return data;
}

export async function createMileageLog(
  householdId: string,
  assetId: string,
  request: CreateMileageLogRequest
): Promise<MileageLogResponse> {
  const { data } = await apiClient.post<MileageLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/mileage-logs`,
    request
  );
  return data;
}

export async function updateMileageLog(
  householdId: string,
  assetId: string,
  mileageLogId: string,
  request: UpdateMileageLogRequest
): Promise<MileageLogResponse> {
  const { data } = await apiClient.put<MileageLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/mileage-logs/${mileageLogId}`,
    request
  );
  return data;
}

export async function deleteMileageLog(
  householdId: string,
  assetId: string,
  mileageLogId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/mileage-logs/${mileageLogId}`
  );
}

export async function listFuelLogs(
  householdId: string,
  assetId: string
): Promise<FuelLogResponse[]> {
  const { data } = await apiClient.get<FuelLogResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/fuel-logs`
  );
  return data;
}

export async function createFuelLog(
  householdId: string,
  assetId: string,
  request: CreateFuelLogRequest
): Promise<FuelLogResponse> {
  const { data } = await apiClient.post<FuelLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/fuel-logs`,
    request
  );
  return data;
}

export async function updateFuelLog(
  householdId: string,
  assetId: string,
  fuelLogId: string,
  request: UpdateFuelLogRequest
): Promise<FuelLogResponse> {
  const { data } = await apiClient.put<FuelLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/fuel-logs/${fuelLogId}`,
    request
  );
  return data;
}

export async function deleteFuelLog(
  householdId: string,
  assetId: string,
  fuelLogId: string
): Promise<void> {
  await apiClient.delete(`/api/households/${householdId}/assets/${assetId}/fuel-logs/${fuelLogId}`);
}

export async function listEngineHoursLogs(
  householdId: string,
  assetId: string,
  engineId: string
): Promise<EngineHoursLogResponse[]> {
  const { data } = await apiClient.get<EngineHoursLogResponse[]>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/hours-logs`
  );
  return data;
}

export async function createEngineHoursLog(
  householdId: string,
  assetId: string,
  engineId: string,
  request: CreateEngineHoursLogRequest
): Promise<EngineHoursLogResponse> {
  const { data } = await apiClient.post<EngineHoursLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/hours-logs`,
    request
  );
  return data;
}

export async function updateEngineHoursLog(
  householdId: string,
  assetId: string,
  engineId: string,
  hoursLogId: string,
  request: UpdateEngineHoursLogRequest
): Promise<EngineHoursLogResponse> {
  const { data } = await apiClient.put<EngineHoursLogResponse>(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/hours-logs/${hoursLogId}`,
    request
  );
  return data;
}

export async function deleteEngineHoursLog(
  householdId: string,
  assetId: string,
  engineId: string,
  hoursLogId: string
): Promise<void> {
  await apiClient.delete(
    `/api/households/${householdId}/assets/${assetId}/engines/${engineId}/hours-logs/${hoursLogId}`
  );
}
