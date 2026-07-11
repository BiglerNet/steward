import { assetPhotoContentUrl } from "@/api/assetPhotos";
import type { PhotoVariant } from "@/api/types";
import { useAuthenticatedImageUrl } from "@/hooks/useAuthenticatedImageUrl";

export interface AssetCoverThumbnailProps {
  householdId: string;
  assetId: string;
  coverPhotoId: string | null;
  alt: string;
  variant?: PhotoVariant;
  className?: string;
}

export function AssetCoverThumbnail({
  householdId,
  assetId,
  coverPhotoId,
  alt,
  variant = "thumb",
  className,
}: AssetCoverThumbnailProps) {
  const url = coverPhotoId ? assetPhotoContentUrl(householdId, assetId, coverPhotoId, variant) : null;
  const objectUrl = useAuthenticatedImageUrl(url);

  if (!objectUrl) {
    return null;
  }

  return <img src={objectUrl} alt={alt} className={className} />;
}
