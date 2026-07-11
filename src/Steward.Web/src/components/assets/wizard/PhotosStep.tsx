import type { AssetResponse } from "@/api/types";
import { PhotosSection } from "@/components/assets/PhotosSection";
import { Button } from "@/components/ui/button";

interface PhotosStepProps {
  asset: AssetResponse;
  onFinish: () => void;
}

export function PhotosStep({ asset, onFinish }: PhotosStepProps) {
  return (
    <div className="space-y-4">
      <PhotosSection asset={asset} />
      <div className="flex justify-end">
        <Button type="button" onClick={onFinish}>
          Finish
        </Button>
      </div>
    </div>
  );
}
