import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatWidget } from "@/components/dashboard/StatWidget";

describe("StatWidget", () => {
  it("centers its label, value, and sub-label content within the card", () => {
    render(<StatWidget label="Cylinder Index" value={24} subLabel="6 active ICE engines" />);

    const value = screen.getByText("24");
    const content = value.parentElement;
    expect(content).toHaveClass("items-center");
    expect(content).toHaveClass("justify-center");
    expect(content).toHaveClass("text-center");

    expect(screen.getByText("6 active ICE engines")).toBeInTheDocument();
  });
});
