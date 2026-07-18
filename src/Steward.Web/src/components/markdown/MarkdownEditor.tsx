import { useEffect, useRef } from "react";
import { Editor, editorViewOptionsCtx, defaultValueCtx, rootCtx } from "@milkdown/kit/core";
import { commonmark } from "@milkdown/kit/preset/commonmark";
import { history } from "@milkdown/kit/plugin/history";
import { listener, listenerCtx } from "@milkdown/kit/plugin/listener";
import { getMarkdown, replaceAll } from "@milkdown/kit/utils";
import { Milkdown, MilkdownProvider, useEditor } from "@milkdown/react";
import "@milkdown/kit/prose/view/style/prosemirror.css";
import { cn } from "@/lib/utils";

export interface MarkdownEditorProps {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  className?: string;
  id?: string;
  "aria-labelledby"?: string;
  "aria-describedby"?: string;
  "aria-invalid"?: boolean;
}

function MarkdownEditorInner({
  value,
  onChange,
  onBlur,
  className,
  id,
  "aria-labelledby": ariaLabelledBy,
  "aria-describedby": ariaDescribedBy,
  "aria-invalid": ariaInvalid,
}: MarkdownEditorProps) {
  const lastEmitted = useRef(value);

  const { get } = useEditor(
    (root) =>
      Editor.make()
        .config((ctx) => {
          ctx.set(rootCtx, root);
          ctx.set(defaultValueCtx, value);
          ctx.update(editorViewOptionsCtx, (prev) => ({
            ...prev,
            attributes: {
              ...prev.attributes,
              id: id ?? "",
              role: "textbox",
              "aria-multiline": "true",
              "aria-labelledby": ariaLabelledBy ?? "",
              "aria-describedby": ariaDescribedBy ?? "",
              "aria-invalid": ariaInvalid ? "true" : "false",
              class: "outline-none",
            },
          }));
          ctx.get(listenerCtx).markdownUpdated((_ctx, markdown, prevMarkdown) => {
            if (markdown !== prevMarkdown) {
              lastEmitted.current = markdown;
              onChange(markdown);
            }
          });
        })
        .use(commonmark)
        .use(history)
        .use(listener),
    // Editor is created once; live prop updates flow through the effect below.
    []
  );

  useEffect(() => {
    if (value === lastEmitted.current) return;
    const editor = get();
    if (!editor) return;
    lastEmitted.current = value;
    editor.action(replaceAll(value));
  }, [value, get]);

  function handleBlur() {
    // The listener plugin's markdownUpdated callback is internally debounced, so a
    // blur that's immediately followed by a form submit (e.g. clicking Save) can
    // race ahead of it and submit a stale value. Flush synchronously here instead.
    const editor = get();
    if (editor) {
      const markdown = editor.action(getMarkdown());
      if (markdown !== lastEmitted.current) {
        lastEmitted.current = markdown;
        onChange(markdown);
      }
    }
    onBlur?.();
  }

  return (
    <div
      onBlur={handleBlur}
      className={cn(
        "markdown-editor min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm transition-colors focus-within:outline-none focus-within:ring-1 focus-within:ring-ring",
        className
      )}
    >
      <Milkdown />
    </div>
  );
}

export function MarkdownEditor(props: MarkdownEditorProps) {
  return (
    <MilkdownProvider>
      <MarkdownEditorInner {...props} />
    </MilkdownProvider>
  );
}
