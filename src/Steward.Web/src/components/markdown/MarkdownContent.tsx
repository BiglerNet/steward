import Markdown from "react-markdown";
import { cn } from "@/lib/utils";

export interface MarkdownContentProps {
  children: string;
  className?: string;
}

export function MarkdownContent({ children, className }: MarkdownContentProps) {
  return (
    <div className={cn("markdown-content text-body", className)}>
      <Markdown>{children}</Markdown>
    </div>
  );
}
