import { useRef, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { downloadDocument, removeDocument, uploadDocument } from "@/api/documents";
import { Button } from "@/components/ui/button";
import { getApiErrorMessage } from "@/lib/apiErrors";
import { validateDocumentFile } from "@/lib/documents";

export interface DocumentAttachmentProps {
  hasDocument: boolean;
  uploadUrl: string;
  downloadUrl: string;
  deleteUrl: string;
  canEdit: boolean;
  onChange: () => void;
}

export function DocumentAttachment({
  hasDocument,
  uploadUrl,
  downloadUrl,
  deleteUrl,
  canEdit,
  onChange,
}: DocumentAttachmentProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadDocument(uploadUrl, file),
    onSuccess: () => {
      setError(null);
      onChange();
      toast.success("Document saved.");
    },
    onError: (mutationError) => {
      toast.error(getApiErrorMessage(mutationError, "Couldn't upload this document."));
    },
  });

  const removeMutation = useMutation({
    mutationFn: () => removeDocument(deleteUrl),
    onSuccess: () => {
      onChange();
      toast.success("Document removed.");
    },
    onError: () => toast.error("Couldn't remove this document."),
  });

  const downloadMutation = useMutation({
    mutationFn: () => downloadDocument(downloadUrl),
    onSuccess: (blob) => {
      const objectUrl = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = objectUrl;
      link.download = "document";
      link.click();
      URL.revokeObjectURL(objectUrl);
    },
    onError: () => toast.error("Couldn't download this document."),
  });

  function handleFileSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }

    const validationError = validateDocumentFile(file);
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    uploadMutation.mutate(file);
  }

  function handleRemove() {
    if (window.confirm("Remove this document?")) {
      removeMutation.mutate();
    }
  }

  return (
    <div className="space-y-1">
      <div className="flex items-center gap-2">
        {hasDocument && (
          <Button
            size="sm"
            variant="outline"
            onClick={() => downloadMutation.mutate()}
            disabled={downloadMutation.isPending}
          >
            Download
          </Button>
        )}
        {!hasDocument && !canEdit && <span className="text-sm text-muted-foreground">No document</span>}
        {canEdit && (
          <>
            <Button
              size="sm"
              variant="outline"
              onClick={() => inputRef.current?.click()}
              disabled={uploadMutation.isPending}
            >
              {hasDocument ? "Replace" : "Attach document"}
            </Button>
            {hasDocument && (
              <Button
                size="sm"
                variant="outline"
                onClick={handleRemove}
                disabled={removeMutation.isPending}
              >
                Remove
              </Button>
            )}
            <input
              ref={inputRef}
              type="file"
              className="hidden"
              accept="application/pdf,image/jpeg,image/png"
              onChange={handleFileSelected}
              aria-label="Document file"
            />
          </>
        )}
      </div>
      {error && <p className="text-sm text-destructive">{error}</p>}
    </div>
  );
}
