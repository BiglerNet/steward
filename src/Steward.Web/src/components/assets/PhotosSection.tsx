import { useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useParams } from "react-router";
import { toast } from "sonner";
import { assetPhotoContentUrl, deleteAssetPhoto, setCoverPhoto, uploadAssetPhoto } from "@/api/assetPhotos";
import type { AssetPhotoResponse, AssetResponse } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import { useAssetPhotos } from "@/hooks/useAssetPhotos";
import { useAuthenticatedImageUrl } from "@/hooks/useAuthenticatedImageUrl";
import { getApiErrorMessage } from "@/lib/apiErrors";
import { validatePhotoFile } from "@/lib/assetPhotos";
import { useHouseholdRole } from "@/lib/permissions";

interface PhotosSectionProps {
  asset: AssetResponse;
}

export function PhotosSection({ asset }: PhotosSectionProps) {
  const { householdId } = useParams() as { householdId: string };
  const assetId = asset.id;
  const { canEdit } = useHouseholdRole();
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [viewing, setViewing] = useState<AssetPhotoResponse | null>(null);

  const queryKey = ["households", householdId, "assets", assetId, "photos"];
  const { data: photos } = useAssetPhotos(householdId, assetId);
  const list = photos ?? [];

  function invalidate() {
    queryClient.invalidateQueries({ queryKey });
    queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets", assetId] });
    queryClient.invalidateQueries({ queryKey: ["households", householdId, "assets"] });
  }

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadAssetPhoto(householdId, assetId, file),
    onSuccess: () => {
      invalidate();
      toast.success("Photo uploaded.");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Couldn't upload this photo.")),
  });

  const deleteMutation = useMutation({
    mutationFn: (photoId: string) => deleteAssetPhoto(householdId, assetId, photoId),
    onSuccess: () => {
      invalidate();
      toast.success("Photo deleted.");
    },
    onError: () => toast.error("Couldn't delete this photo."),
  });

  const coverMutation = useMutation({
    mutationFn: (photoId: string) => setCoverPhoto(householdId, assetId, photoId),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't set this photo as cover."),
  });

  function handleFileSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) {
      return;
    }

    const validationError = validatePhotoFile(file);
    if (validationError) {
      toast.error(validationError);
      return;
    }

    uploadMutation.mutate(file);
  }

  function handleDelete(photo: AssetPhotoResponse) {
    if (window.confirm("Delete this photo?")) {
      deleteMutation.mutate(photo.id);
    }
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-h2">Photos</h2>
        {canEdit && (
          <>
            <Button
              size="sm"
              onClick={() => inputRef.current?.click()}
              disabled={uploadMutation.isPending}
            >
              {uploadMutation.isPending ? "Uploading…" : "Add photo"}
            </Button>
            <input
              ref={inputRef}
              type="file"
              className="hidden"
              accept="image/jpeg,image/png,image/webp"
              onChange={handleFileSelected}
              aria-label="Photo file"
            />
          </>
        )}
      </div>

      {list.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No photos yet.{canEdit ? " Add one to see it here." : ""}
        </p>
      ) : (
        <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-6">
          {list.map((photo) => (
            <PhotoThumbnail
              key={photo.id}
              householdId={householdId}
              assetId={assetId}
              photo={photo}
              isCover={asset.coverPhotoId === photo.id}
              canEdit={canEdit}
              onView={() => setViewing(photo)}
              onDelete={() => handleDelete(photo)}
              onSetCover={() => coverMutation.mutate(photo.id)}
            />
          ))}
        </div>
      )}

      <Dialog open={!!viewing} onOpenChange={(open) => !open && setViewing(null)}>
        <DialogContent className="max-w-3xl">
          {viewing && <PhotoDisplayImage householdId={householdId} assetId={assetId} photo={viewing} />}
        </DialogContent>
      </Dialog>
    </div>
  );
}

interface PhotoThumbnailProps {
  householdId: string;
  assetId: string;
  photo: AssetPhotoResponse;
  isCover: boolean;
  canEdit: boolean;
  onView: () => void;
  onDelete: () => void;
  onSetCover: () => void;
}

function PhotoThumbnail({
  householdId,
  assetId,
  photo,
  isCover,
  canEdit,
  onView,
  onDelete,
  onSetCover,
}: PhotoThumbnailProps) {
  const url = assetPhotoContentUrl(householdId, assetId, photo.id, "thumb");
  const objectUrl = useAuthenticatedImageUrl(url);

  return (
    <div className="group relative">
      <button
        type="button"
        onClick={onView}
        className="block aspect-square w-full overflow-hidden rounded-md border border-border bg-muted"
      >
        {objectUrl && <img src={objectUrl} alt="Asset photo" className="h-full w-full object-cover" />}
      </button>
      {isCover && (
        <span className="absolute left-1 top-1 rounded bg-primary px-1.5 py-0.5 text-xs font-medium text-primary-foreground">
          Cover
        </span>
      )}
      {canEdit && (
        <div className="absolute inset-x-0 bottom-0 flex justify-center gap-1 bg-black/50 p-1 opacity-0 transition-opacity group-hover:opacity-100">
          {!isCover && (
            <button
              type="button"
              onClick={onSetCover}
              className="rounded px-1.5 py-0.5 text-xs text-white hover:bg-white/20"
            >
              Set cover
            </button>
          )}
          <button
            type="button"
            onClick={onDelete}
            className="rounded px-1.5 py-0.5 text-xs text-white hover:bg-white/20"
          >
            Delete
          </button>
        </div>
      )}
    </div>
  );
}

interface PhotoDisplayImageProps {
  householdId: string;
  assetId: string;
  photo: AssetPhotoResponse;
}

function PhotoDisplayImage({ householdId, assetId, photo }: PhotoDisplayImageProps) {
  const url = assetPhotoContentUrl(householdId, assetId, photo.id, "display");
  const objectUrl = useAuthenticatedImageUrl(url);

  if (!objectUrl) {
    return <p className="py-8 text-center text-body text-muted-foreground">Loading…</p>;
  }

  return (
    <img src={objectUrl} alt="Asset photo" className="max-h-[80vh] w-full rounded-md object-contain" />
  );
}
