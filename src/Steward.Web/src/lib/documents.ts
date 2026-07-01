export const ALLOWED_DOCUMENT_TYPES = ["application/pdf", "image/jpeg", "image/png"];
export const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024;

export function validateDocumentFile(file: File): string | null {
  if (!ALLOWED_DOCUMENT_TYPES.includes(file.type)) {
    return "Unsupported file type. Allowed: PDF, JPEG, PNG.";
  }

  if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
    return "File exceeds the maximum allowed size (10 MB).";
  }

  return null;
}
