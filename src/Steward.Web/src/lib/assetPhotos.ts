export const ALLOWED_PHOTO_TYPES = ["image/jpeg", "image/png", "image/webp"];
export const MAX_PHOTO_SIZE_BYTES = 15 * 1024 * 1024;

export function validatePhotoFile(file: File): string | null {
  if (!ALLOWED_PHOTO_TYPES.includes(file.type)) {
    return "Unsupported file type. Allowed: JPEG, PNG, WebP.";
  }

  if (file.size > MAX_PHOTO_SIZE_BYTES) {
    return "File exceeds the maximum allowed size (15 MB).";
  }

  return null;
}
