import { useEffect, useRef, useState } from "react";
import { Crepe } from "@milkdown/crepe";
import { editorViewOptionsCtx, EditorStatus } from "@milkdown/kit/core";
import { replaceAll } from "@milkdown/kit/utils";
import { Milkdown, MilkdownProvider, useEditor } from "@milkdown/react";
import "@milkdown/kit/prose/view/style/prosemirror.css";
import "@milkdown/crepe/theme/common/style.css";
import "@milkdown/crepe/theme/frame.css";
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

type EditorMode = "wysiwyg" | "source";

// Raw SVG markup mirroring lucide-react's icon style (24x24, stroke-based),
// since Crepe's TopBar icon overrides take HTML strings, not React nodes.
function lucideIcon(inner: string) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${inner}</svg>`;
}

const topBarIcons = {
  boldIcon: lucideIcon('<path d="M14 12a4 4 0 0 0 0-8H6v8"/><path d="M15 20a4 4 0 0 0 0-8H6v8Z"/>'),
  italicIcon: lucideIcon(
    '<line x1="19" x2="10" y1="4" y2="4"/><line x1="14" x2="5" y1="20" y2="20"/><line x1="15" x2="9" y1="4" y2="20"/>'
  ),
  bulletListIcon: lucideIcon(
    '<path d="M3 12h.01"/><path d="M3 18h.01"/><path d="M3 6h.01"/><path d="M8 12h13"/><path d="M8 18h13"/><path d="M8 6h13"/>'
  ),
  orderedListIcon: lucideIcon(
    '<path d="M10 12h11"/><path d="M10 18h11"/><path d="M10 6h11"/><path d="M4 10h2"/><path d="M4 6h1v4"/><path d="M6 18H4c0-1 2-2 2-3s-1-1.5-2-1"/>'
  ),
  linkIcon: lucideIcon(
    '<path d="M9 17H7A5 5 0 0 1 7 7h2"/><path d="M15 7h2a5 5 0 1 1 0 10h-2"/><line x1="8" x2="16" y1="12" y2="12"/>'
  ),
  chevronDownIcon: lucideIcon('<path d="m6 9 6 6 6-6"/>'),
};

interface CrepeSurfaceProps {
  value: string;
  onChange: (value: string) => void;
  lastEmitted: React.MutableRefObject<string>;
  crepeRef: React.MutableRefObject<Crepe | null>;
  id?: string;
  ariaLabelledBy?: string;
  ariaDescribedBy?: string;
  ariaInvalid?: boolean;
}

function CrepeSurface({
  value,
  onChange,
  lastEmitted,
  crepeRef,
  id,
  ariaLabelledBy,
  ariaDescribedBy,
  ariaInvalid,
}: CrepeSurfaceProps) {
  useEditor(
    (root) => {
      const crepe = new Crepe({
        root,
        defaultValue: value,
        features: {
          [Crepe.Feature.TopBar]: true,
          // Floating bubble toolbar and the "/" block-insert menu are explicitly
          // rejected discovery patterns for this component (see design.md) - the
          // fixed TopBar is the only formatting affordance.
          [Crepe.Feature.Toolbar]: false,
          [Crepe.Feature.BlockEdit]: false,
          // Table/Latex/ImageBlock/CodeMirror would expand markdown scope beyond
          // what this component has ever supported (design.md non-goals).
          [Crepe.Feature.Table]: false,
          [Crepe.Feature.Latex]: false,
          [Crepe.Feature.ImageBlock]: false,
          [Crepe.Feature.CodeMirror]: false,
          // Renders each <li> as a flex row with a fixed 32px bullet/checkbox
          // box, which reads as oversized double-spacing in a compact form
          // field. Plain <li> markup (styled by index.css) matches the rest
          // of this component's minimal-footprint list rendering.
          [Crepe.Feature.ListItem]: false,
        },
        featureConfigs: {
          [Crepe.Feature.TopBar]: {
            headingOptions: [
              { label: "Text", level: null },
              { label: "H1", level: 1 },
              { label: "H2", level: 2 },
              { label: "H3", level: 3 },
              { label: "H4", level: 4 },
              { label: "H5", level: 5 },
              { label: "H6", level: 6 },
            ],
            ...topBarIcons,
          },
        },
      });

      crepe.editor.config((ctx) => {
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
            class: cn(prev.attributes && "class" in prev.attributes ? String(prev.attributes.class) : "", "outline-none"),
          },
        }));
      });

      crepe.on((listener) => {
        listener.markdownUpdated((_ctx, markdown, prevMarkdown) => {
          if (markdown !== prevMarkdown) {
            lastEmitted.current = markdown;
            onChange(markdown);
          }
        });
      });

      // Crepe's own `.create()` resolves with the underlying kit Editor, not the
      // Crepe wrapper, so useEditor's `get()` can't be used to reach `getMarkdown()`
      // et al. - stash the wrapper directly instead.
      crepeRef.current = crepe;
      return crepe;
    },
    // Crepe instance is created once per mount; live prop updates flow through
    // the parent's sync effect via crepeRef, not through this factory.
    []
  );

  useEffect(() => {
    return () => {
      crepeRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return <Milkdown />;
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
  const [mode, setMode] = useState<EditorMode>("wysiwyg");
  const lastEmitted = useRef(value);
  const crepeRef = useRef<Crepe | null>(null);

  useEffect(() => {
    if (mode !== "wysiwyg") return;
    if (value === lastEmitted.current) return;
    const crepe = crepeRef.current;
    // Mirrors the readiness check useEditor's own `get()` does internally: skip
    // silently if Crepe's async creation hasn't resolved yet (e.g. an external
    // value change lands in the same tick this mode's editor was just mounted).
    if (!crepe || crepe.editor.status !== EditorStatus.Created) return;
    lastEmitted.current = value;
    crepe.editor.action(replaceAll(value));
  }, [value, mode]);

  function flushWysiwyg() {
    const crepe = crepeRef.current;
    if (!crepe || crepe.editor.status !== EditorStatus.Created) return;
    const markdown = crepe.getMarkdown();
    if (markdown !== lastEmitted.current) {
      lastEmitted.current = markdown;
      onChange(markdown);
    }
  }

  function handleBlur() {
    // Crepe's markdownUpdated listener is internally debounced, so a blur
    // immediately followed by a form submit (e.g. clicking Save) can race
    // ahead of it and submit a stale value. Flush synchronously here instead.
    if (mode === "wysiwyg") flushWysiwyg();
    onBlur?.();
  }

  function handleModeChange(nextMode: EditorMode) {
    if (nextMode === mode) return;
    if (mode === "wysiwyg") flushWysiwyg();
    setMode(nextMode);
  }

  return (
    <div className={cn("markdown-editor", className)}>
      <div className="mb-2 inline-flex rounded-md bg-muted p-1 text-muted-foreground">
        <button
          type="button"
          onClick={() => handleModeChange("wysiwyg")}
          aria-pressed={mode === "wysiwyg"}
          className={cn(
            "rounded-sm px-3 py-1 text-sm font-medium transition-colors",
            mode === "wysiwyg" ? "bg-background text-foreground shadow" : "hover:text-foreground"
          )}
        >
          Rich text
        </button>
        <button
          type="button"
          onClick={() => handleModeChange("source")}
          aria-pressed={mode === "source"}
          className={cn(
            "rounded-sm px-3 py-1 text-sm font-medium transition-colors",
            mode === "source" ? "bg-background text-foreground shadow" : "hover:text-foreground"
          )}
        >
          Markdown
        </button>
      </div>

      {mode === "wysiwyg" ? (
        <div
          onBlur={handleBlur}
          className="min-h-48 w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm transition-colors focus-within:outline-none focus-within:ring-1 focus-within:ring-ring"
        >
          <CrepeSurface
            value={value}
            onChange={onChange}
            lastEmitted={lastEmitted}
            crepeRef={crepeRef}
            id={id}
            ariaLabelledBy={ariaLabelledBy}
            ariaDescribedBy={ariaDescribedBy}
            ariaInvalid={ariaInvalid}
          />
        </div>
      ) : (
        <textarea
          id={id}
          aria-labelledby={ariaLabelledBy}
          aria-describedby={ariaDescribedBy}
          aria-invalid={ariaInvalid}
          value={value}
          onChange={(event) => {
            lastEmitted.current = event.target.value;
            onChange(event.target.value);
          }}
          onBlur={handleBlur}
          className="min-h-48 w-full resize-y rounded-md border border-input bg-background px-3 py-2 font-mono text-sm shadow-sm transition-colors outline-none focus:ring-1 focus:ring-ring"
        />
      )}
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
