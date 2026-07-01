export const DUE_SOON_DAYS = 30;

export type ExpiryStatus = "overdue" | "dueSoon" | "ok" | "none";

export function getExpiryStatus(expiresOn: string | null | undefined): ExpiryStatus {
  if (!expiresOn) {
    return "none";
  }

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const [year, month, day] = expiresOn.split("-").map(Number);
  const expiry = new Date(year, month - 1, day);

  const diffDays = Math.round((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

  if (diffDays < 0) {
    return "overdue";
  }

  if (diffDays <= DUE_SOON_DAYS) {
    return "dueSoon";
  }

  return "ok";
}
