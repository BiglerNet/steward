import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TypeStep } from "@/components/assets/wizard/TypeStep";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";

describe("TypeStep", () => {
  it("groups categories under their registry group headings", () => {
    render(
      <TypeStep registry={testAssetTypeRegistry} selectedCategory={undefined} onSelect={vi.fn()} onContinue={vi.fn()} />
    );

    expect(screen.getByText("Road")).toBeInTheDocument();
    expect(screen.getByText("Powersports")).toBeInTheDocument();
    expect(screen.getByText("Water")).toBeInTheDocument();
    expect(screen.getByText("Equipment")).toBeInTheDocument();
    expect(screen.getByText("Trailers")).toBeInTheDocument();
  });

  it("renders every registry category as a radio row", () => {
    render(
      <TypeStep registry={testAssetTypeRegistry} selectedCategory={undefined} onSelect={vi.fn()} onContinue={vi.fn()} />
    );

    const radios = screen.getAllByRole("radio");
    expect(radios).toHaveLength(testAssetTypeRegistry.length);
    for (const definition of testAssetTypeRegistry) {
      expect(screen.getByRole("radio", { name: definition.displayLabel })).toBeInTheDocument();
    }
  });

  it("calls onSelect when a row is clicked and reflects the current selection", async () => {
    const onSelect = vi.fn();
    const user = userEvent.setup();
    const { rerender } = render(
      <TypeStep registry={testAssetTypeRegistry} selectedCategory={undefined} onSelect={onSelect} onContinue={vi.fn()} />
    );

    await user.click(screen.getByRole("radio", { name: "Power Boat" }));
    expect(onSelect).toHaveBeenCalledWith("PowerBoat");

    rerender(
      <TypeStep registry={testAssetTypeRegistry} selectedCategory="PowerBoat" onSelect={onSelect} onContinue={vi.fn()} />
    );
    expect(screen.getByRole("radio", { name: "Power Boat" })).toHaveAttribute("aria-checked", "true");
    expect(screen.getByRole("radio", { name: "Car" })).toHaveAttribute("aria-checked", "false");
  });

  it("disables Continue until a category is selected", () => {
    render(
      <TypeStep registry={testAssetTypeRegistry} selectedCategory={undefined} onSelect={vi.fn()} onContinue={vi.fn()} />
    );

    expect(screen.getByRole("button", { name: "Continue" })).toBeDisabled();
  });
});
