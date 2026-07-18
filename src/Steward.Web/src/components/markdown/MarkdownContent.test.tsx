import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { MarkdownContent } from "@/components/markdown/MarkdownContent";

describe("MarkdownContent", () => {
  it("renders headings and lists as formatted elements", () => {
    render(<MarkdownContent>{"# Heading\n\n- one\n- two"}</MarkdownContent>);

    expect(screen.getByRole("heading", { level: 1, name: "Heading" })).toBeInTheDocument();
    expect(screen.getByRole("list")).toBeInTheDocument();
    expect(screen.getByText("one")).toBeInTheDocument();
    expect(screen.getByText("two")).toBeInTheDocument();
  });

  it("does not execute or render embedded script/HTML as live markup", () => {
    const { container } = render(
      <MarkdownContent>{'Before <script>window.__pwned = true;</script> after'}</MarkdownContent>
    );

    expect(container.querySelector("script")).not.toBeInTheDocument();
    expect((window as unknown as { __pwned?: boolean }).__pwned).toBeUndefined();
  });
});
