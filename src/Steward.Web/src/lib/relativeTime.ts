const rtf = new Intl.RelativeTimeFormat("en", { numeric: "auto" });

export function formatRelativeToNow(date: Date, now = new Date()): string {
  const diffMs = date.getTime() - now.getTime();
  const diffMinutes = Math.round(diffMs / (1000 * 60));
  if (Math.abs(diffMinutes) < 60) {
    return rtf.format(diffMinutes, "minute");
  }
  const diffHours = Math.round(diffMs / (1000 * 60 * 60));
  if (Math.abs(diffHours) < 24) {
    return rtf.format(diffHours, "hour");
  }
  const diffDays = Math.round(diffMs / (1000 * 60 * 60 * 24));
  return rtf.format(diffDays, "day");
}
