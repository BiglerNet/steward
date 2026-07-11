import { useEffect, useState } from "react";
import { downloadDocument } from "@/api/documents";

/// Fetches an authenticated image endpoint as a blob and exposes it as an object URL,
/// revoking the previous URL whenever the source URL changes or the component unmounts.
export function useAuthenticatedImageUrl(url: string | null | undefined): string | undefined {
  const [objectUrl, setObjectUrl] = useState<string>();

  useEffect(() => {
    if (!url) {
      return;
    }

    let cancelled = false;
    let createdUrl: string | undefined;

    downloadDocument(url).then((blob) => {
      if (cancelled) {
        return;
      }
      createdUrl = URL.createObjectURL(blob);
      setObjectUrl(createdUrl);
    });

    return () => {
      cancelled = true;
      setObjectUrl(undefined);
      if (createdUrl) {
        URL.revokeObjectURL(createdUrl);
      }
    };
  }, [url]);

  return objectUrl;
}
