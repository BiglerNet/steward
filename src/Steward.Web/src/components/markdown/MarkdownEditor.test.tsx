import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MarkdownEditor } from "@/components/markdown/MarkdownEditor";

describe("MarkdownEditor", () => {
  it("loads an existing markdown string in its WYSIWYG form", async () => {
    render(<MarkdownEditor value={"# Hello\n\n**bold**"} onChange={vi.fn()} id="notes" />);

    expect(await screen.findByRole("heading", { level: 1, name: "Hello" })).toBeInTheDocument();
    expect(screen.getByText("bold").tagName.toLowerCase()).toBe("strong");
  });

  it("emits markdown text as the user types", async () => {
    const onChange = vi.fn();
    render(<MarkdownEditor value="" onChange={onChange} id="notes" />);
    const user = userEvent.setup();

    const editor = await screen.findByRole("textbox");
    await user.click(editor);
    await user.type(editor, "hello world");

    await waitFor(() => expect(onChange).toHaveBeenCalledWith(expect.stringContaining("hello world")));
  });

  it("flushes the latest markdown synchronously on blur, ahead of the debounced listener", async () => {
    const onChange = vi.fn();
    render(
      <>
        <MarkdownEditor value="" onChange={onChange} id="notes" />
        <button type="button">elsewhere</button>
      </>
    );
    const user = userEvent.setup();

    const editor = await screen.findByRole("textbox");
    await user.click(editor);
    await user.type(editor, "hello world");
    onChange.mockClear();

    // Blurring immediately after typing (e.g. clicking a Save button) must not lose
    // the just-typed content to the listener plugin's internal ~200ms debounce.
    await user.click(screen.getByRole("button", { name: "elsewhere" }));

    expect(onChange).toHaveBeenCalledWith(expect.stringContaining("hello world"));
  });
});
