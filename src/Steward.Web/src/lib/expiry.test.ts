import { describe, expect, it } from "vitest";
import { getExpiryStatus } from "@/lib/expiry";

function isoDateOffset(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

describe("getExpiryStatus", () => {
  it("returns none when there is no expiry date", () => {
    expect(getExpiryStatus(null)).toBe("none");
    expect(getExpiryStatus(undefined)).toBe("none");
  });

  it("returns overdue for a date in the past", () => {
    expect(getExpiryStatus(isoDateOffset(-1))).toBe("overdue");
  });

  it("returns dueSoon for a date within the coming-due window", () => {
    expect(getExpiryStatus(isoDateOffset(0))).toBe("dueSoon");
    expect(getExpiryStatus(isoDateOffset(30))).toBe("dueSoon");
  });

  it("returns ok for a date beyond the coming-due window", () => {
    expect(getExpiryStatus(isoDateOffset(31))).toBe("ok");
  });
});
