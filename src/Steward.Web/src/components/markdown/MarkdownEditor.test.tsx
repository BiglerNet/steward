import { useState } from "react";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { MarkdownEditor, type MarkdownEditorProps } from "@/components/markdown/MarkdownEditor";

// Mirrors how react-hook-form's Controller drives MarkdownEditor: onChange feeds
// straight back into the next render's `value` prop.
function ControlledMarkdownEditor(props: Omit<MarkdownEditorProps, "value" | "onChange"> & { initialValue?: string }) {
  const { initialValue = "", ...rest } = props;
  const [value, setValue] = useState(initialValue);
  return <MarkdownEditor {...rest} value={value} onChange={setValue} />;
}

// Crepe's LinkTooltip feature renders its own <input role="textbox"> up front, so
// role-based lookup alone is ambiguous - scope to the actual editable surface by id.
async function findWysiwygEditor(container: HTMLElement, id: string) {
  return waitFor(() => {
    const el = container.querySelector<HTMLElement>(`#${id}[contenteditable="true"]`);
    if (!el) throw new Error("WYSIWYG editor not ready");
    return el;
  });
}

function getTopBarButton(container: HTMLElement, firstPathD: string) {
  const path = Array.from(container.querySelectorAll(".top-bar-item path")).find(
    (p) => p.getAttribute("d") === firstPathD
  );
  const button = path?.closest("button");
  if (!button) throw new Error(`toolbar button containing path "${firstPathD}" not found`);
  return button as HTMLButtonElement;
}

const boldButtonPathD = "M14 12a4 4 0 0 0 0-8H6v8";

// ProseMirror doesn't rely on jsdom's (nonexistent) native contenteditable caret/selection
// behavior - it reads the DOM Selection/Range API directly, so tests drive selection the
// same way a real browser would report it: build a Range over the target text and fire
// `selectionchange` so the editor view picks it up.
function selectText(container: HTMLElement, text: string) {
  const walker = document.createTreeWalker(container, NodeFilter.SHOW_TEXT);
  let node: Text | null;
  while ((node = walker.nextNode() as Text | null)) {
    const index = node.textContent?.indexOf(text) ?? -1;
    if (index !== -1) {
      const range = document.createRange();
      range.setStart(node, index);
      range.setEnd(node, index + text.length);
      const selection = window.getSelection();
      selection?.removeAllRanges();
      selection?.addRange(range);
      document.dispatchEvent(new Event("selectionchange"));
      return;
    }
  }
  throw new Error(`text "${text}" not found to select`);
}

describe("MarkdownEditor", () => {
  it("loads an existing markdown string in its WYSIWYG form", async () => {
    render(<MarkdownEditor value={"# Hello\n\n**bold**"} onChange={vi.fn()} id="notes" />);

    expect(await screen.findByRole("heading", { level: 1, name: "Hello" })).toBeInTheDocument();
    expect(screen.getByText("bold").tagName.toLowerCase()).toBe("strong");
  });

  it("emits markdown text as the user types", async () => {
    const onChange = vi.fn();
    const { container } = render(<MarkdownEditor value="" onChange={onChange} id="notes" />);
    const user = userEvent.setup();

    const editor = await findWysiwygEditor(container, "notes");
    await user.click(editor);
    await user.type(editor, "hello world");

    await waitFor(() => expect(onChange).toHaveBeenCalledWith(expect.stringContaining("hello world")));
  });

  it("flushes the latest markdown synchronously on blur, ahead of the debounced listener", async () => {
    const onChange = vi.fn();
    const { container } = render(
      <>
        <MarkdownEditor value="" onChange={onChange} id="notes" />
        <button type="button">elsewhere</button>
      </>
    );
    const user = userEvent.setup();

    const editor = await findWysiwygEditor(container, "notes");
    await user.click(editor);
    await user.type(editor, "hello world");
    onChange.mockClear();

    // Blurring immediately after typing (e.g. clicking a Save button) must not lose
    // the just-typed content to the listener plugin's internal ~200ms debounce.
    await user.click(screen.getByRole("button", { name: "elsewhere" }));

    expect(onChange).toHaveBeenCalledWith(expect.stringContaining("hello world"));
  });

  it("produces formatted markdown from the toolbar's Bold control without typed syntax", async () => {
    const onChange = vi.fn();
    const { container } = render(<MarkdownEditor value="" onChange={onChange} id="notes" />);
    const user = userEvent.setup();

    const editor = await findWysiwygEditor(container, "notes");
    await user.click(editor);
    await user.type(editor, "hello world");
    selectText(container, "world");

    await user.click(getTopBarButton(container, boldButtonPathD));

    await waitFor(() => expect(onChange).toHaveBeenCalledWith(expect.stringContaining("**world**")));
  });

  it("prefixes the current line with the correct heading markdown from the toolbar", async () => {
    const onChange = vi.fn();
    const { container } = render(<MarkdownEditor value="" onChange={onChange} id="notes" />);
    const user = userEvent.setup();

    const editor = await findWysiwygEditor(container, "notes");
    await user.click(editor);
    await user.type(editor, "hello world");

    await user.click(screen.getByRole("button", { name: "Text" }));
    await user.click(await screen.findByText("H1"));

    await waitFor(() => expect(onChange).toHaveBeenCalledWith(expect.stringContaining("# hello world")));
  });

  it("defaults to WYSIWYG mode on mount, empty or with an existing value", async () => {
    const { container: emptyContainer } = render(<MarkdownEditor value="" onChange={vi.fn()} id="notes" />);
    expect(await findWysiwygEditor(emptyContainer, "notes")).toBeInTheDocument();
    expect(within(emptyContainer).getByRole("button", { name: "Rich text" })).toHaveAttribute(
      "aria-pressed",
      "true"
    );

    const { container: filledContainer } = render(
      <MarkdownEditor value="# Hello" onChange={vi.fn()} id="notes2" />
    );
    expect(await findWysiwygEditor(filledContainer, "notes2")).toBeInTheDocument();
  });

  it("shows the literal markdown in source mode after producing formatted WYSIWYG content", async () => {
    const { container } = render(<ControlledMarkdownEditor id="notes" />);
    const user = userEvent.setup();

    const editor = await findWysiwygEditor(container, "notes");
    await user.click(editor);
    await user.type(editor, "hello world");
    selectText(container, "world");
    await user.click(getTopBarButton(container, boldButtonPathD));
    await waitFor(() =>
      expect(container.querySelector(".ProseMirror strong")?.textContent).toBe("world")
    );

    await user.click(screen.getByRole("button", { name: "Markdown" }));

    const textarea = await screen.findByRole("textbox");
    expect((textarea as HTMLTextAreaElement).value).toContain("**world**");
  });

  it("renders formatted output in WYSIWYG mode after typing markdown syntax in source mode", async () => {
    render(<ControlledMarkdownEditor id="notes" />);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Markdown" }));
    const textarea = screen.getByRole("textbox");
    await user.type(textarea, "- item");

    await user.click(screen.getByRole("button", { name: "Rich text" }));

    expect(await screen.findByRole("listitem")).toHaveTextContent("item");
  });

  it("keeps an unsaved edit visible after toggling modes without a blur or save", async () => {
    render(<ControlledMarkdownEditor id="notes" />);
    const user = userEvent.setup();

    await user.click(screen.getByRole("button", { name: "Markdown" }));
    const textarea = screen.getByRole("textbox");
    await user.type(textarea, "hello world");

    await user.click(screen.getByRole("button", { name: "Rich text" }));

    expect(await screen.findByText("hello world")).toBeInTheDocument();
  });
});
