import { apiClient } from "@/api/client";

export async function uploadDocument<T>(url: string, file: File): Promise<T> {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await apiClient.post<T>(url, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}

export async function downloadDocument(url: string): Promise<Blob> {
  const { data } = await apiClient.get<Blob>(url, { responseType: "blob" });
  return data;
}

export async function removeDocument(url: string): Promise<void> {
  await apiClient.delete(url);
}
