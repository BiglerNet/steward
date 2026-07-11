import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AssetTypeIcon } from "@/components/assets/AssetTypeIcon";
import { testAssetTypeRegistry } from "@/test-fixtures/assetTypes";

describe("AssetTypeIcon", () => {
  it.each(testAssetTypeRegistry)("resolves a real lucide icon for $category ($icon)", (definition) => {
    const { container } = render(<AssetTypeIcon icon={definition.icon} group={definition.group} />);
    const svg = container.querySelector("svg");
    expect(svg).toBeTruthy();
    expect(svg?.classList.contains("lucide-box")).toBe(false);
  });

  it("renders the neutral fallback icon for an unrecognized name", () => {
    const { container } = render(<AssetTypeIcon icon="not-a-real-icon" group="Road" />);
    const svg = container.querySelector("svg");
    expect(svg?.classList.contains("lucide-box")).toBe(true);
  });

  it("applies the group tint chip classes", () => {
    const { container } = render(<AssetTypeIcon icon="sailboat" group="Water" />);
    const chip = container.querySelector("span");
    expect(chip?.className).toContain("bg-asset-chip-water-bg");
    expect(chip?.className).toContain("text-asset-chip-water-fg");
  });
});
