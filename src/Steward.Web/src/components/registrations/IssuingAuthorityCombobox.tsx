import { useEffect, useRef, useState, type ComponentProps } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface IssuingAuthorityComboboxProps extends Omit<ComponentProps<"input">, "value" | "onChange" | "onBlur"> {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  suggestions: string[];
}

export function IssuingAuthorityCombobox({
  value,
  onChange,
  onBlur,
  suggestions,
  ...inputProps
}: IssuingAuthorityComboboxProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const query = value.trim().toLowerCase();
  const filtered = query === ""
    ? suggestions
    : suggestions.filter((suggestion) => suggestion.toLowerCase().includes(query));

  return (
    <div className="relative" ref={containerRef}>
      <Input
        value={value}
        onChange={(event) => {
          onChange(event.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={onBlur}
        autoComplete="off"
        {...inputProps}
      />
      {open && filtered.length > 0 && (
        <ul className="absolute z-10 mt-1 max-h-56 w-full overflow-auto rounded-md border border-border bg-popover shadow-md">
          {filtered.map((suggestion) => (
            <li key={suggestion}>
              <button
                type="button"
                className={cn(
                  "w-full px-3 py-1.5 text-left text-sm hover:bg-muted",
                  suggestion === value && "bg-muted"
                )}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => {
                  onChange(suggestion);
                  setOpen(false);
                }}
              >
                {suggestion}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
