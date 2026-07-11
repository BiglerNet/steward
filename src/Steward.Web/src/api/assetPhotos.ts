import { apiClient } from "@/api/client";
import { downloadDocument, removeDocument, uploadDocument } from "@/api/documents";
import type { AssetPhotoResponse, AssetResponse, PhotoVariant, SetCoverPhotoRequest } from "@/api/types";

function photosUrl(householdId: string, assetId: string): string {
  return `/api/households/${householdId}/assets/${assetId}/photos`;
}

export async function listAssetPhotos(
  householdId: string,
  assetId: string
): Promise<AssetPhotoResponse[]> {
  const { data } = await apiClient.get<AssetPhotoResponse[]>(photosUrl(householdId, assetId));
  return data;
}

export async function uploadAssetPhoto(
  householdId: string,
  assetId: string,
  file: File
): Promise<AssetPhotoResponse> {
  return uploadDocument<AssetPhotoResponse>(photosUrl(householdId, assetId), file);
}

export async function deleteAssetPhoto(
  householdId: string,
  assetId: string,
  photoId: string
): Promise<void> {
  await removeDocument(`${photosUrl(householdId, assetId)}/${photoId}`);
}

export function assetPhotoContentUrl(
  householdId: string,
  assetId: string,
  photoId: string,
  variant: PhotoVariant
): string {
  return `${photosUrl(householdId, assetId)}/${photoId}/content?variant=${variant}`;
}

export async function downloadAssetPhoto(
  householdId: string,
  assetId: string,
  photoId: string,
  variant: PhotoVariant
): Promise<Blob> {
  return downloadDocument(assetPhotoContentUrl(householdId, assetId, photoId, variant));
}

export async function setCoverPhoto(
  householdId: string,
  assetId: string,
  photoId: string
): Promise<AssetResponse> {
  const { data } = await apiClient.put<AssetResponse>(
    `/api/households/${householdId}/assets/${assetId}/cover-photo`,
    { photoId } satisfies SetCoverPhotoRequest
  );
  return data;
}
