import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StorageUsageSummary } from "@/components/households/StorageUsageSummary";

describe("StorageUsageSummary", () => {
  it("renders a human-readable usage summary against the quota", () => {
    render(<StorageUsageSummary usedBytes={412 * 1024 * 1024} quotaBytes={1024 * 1024 * 1024} />);

    expect(screen.getByText("412 MB of 1 GB")).toBeInTheDocument();
    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "40");
    expect(screen.queryByText(/almost full/i)).not.toBeInTheDocument();
  });

  it("switches to a warning treatment at or above 90% usage", () => {
    render(<StorageUsageSummary usedBytes={950 * 1024 * 1024} quotaBytes={1024 * 1024 * 1024} />);

    expect(screen.getByText(/almost full/i)).toBeInTheDocument();
    const bar = screen.getByRole("progressbar").firstElementChild;
    expect(bar).toHaveClass("bg-warning");
  });

  it("caps the progress bar at 100% when usage exceeds the quota", () => {
    render(<StorageUsageSummary usedBytes={2 * 1024 * 1024 * 1024} quotaBytes={1024 * 1024 * 1024} />);

    expect(screen.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "100");
  });
});
