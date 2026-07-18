import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";

interface DoneTransitionDialogProps {
  open: boolean;
  openItemCount: number;
  pending: boolean;
  onGoBack: () => void;
  onSkipRemainingThenComplete: () => void;
  onCompleteAnyway: () => void;
}

export function DoneTransitionDialog({
  open,
  openItemCount,
  pending,
  onGoBack,
  onSkipRemainingThenComplete,
  onCompleteAnyway,
}: DoneTransitionDialogProps) {
  return (
    <Dialog open={open} onOpenChange={(next) => !next && onGoBack()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Complete with open items?</DialogTitle>
        </DialogHeader>
        <p className="text-sm text-muted-foreground">
          {openItemCount} checklist item{openItemCount === 1 ? " is" : "s are"} still open.
        </p>
        <DialogFooter className="sm:flex-col sm:items-stretch sm:gap-2">
          <Button variant="outline" onClick={onGoBack} disabled={pending}>
            Go back
          </Button>
          <Button variant="outline" onClick={onCompleteAnyway} disabled={pending}>
            Complete anyway
          </Button>
          <Button onClick={onSkipRemainingThenComplete} disabled={pending}>
            Mark remaining as Skipped, then complete
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
