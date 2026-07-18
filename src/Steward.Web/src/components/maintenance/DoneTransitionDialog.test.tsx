import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DoneTransitionDialog } from "@/components/maintenance/DoneTransitionDialog";

describe("DoneTransitionDialog", () => {
  it("calls onGoBack and does not modify anything", async () => {
    const onGoBack = vi.fn();
    const onCompleteAnyway = vi.fn();
    const onSkip = vi.fn();
    const user = userEvent.setup();

    render(
      <DoneTransitionDialog
        open
        openItemCount={2}
        pending={false}
        onGoBack={onGoBack}
        onCompleteAnyway={onCompleteAnyway}
        onSkipRemainingThenComplete={onSkip}
      />
    );

    await user.click(screen.getByRole("button", { name: "Go back" }));
    expect(onGoBack).toHaveBeenCalled();
    expect(onCompleteAnyway).not.toHaveBeenCalled();
    expect(onSkip).not.toHaveBeenCalled();
  });

  it("calls onCompleteAnyway when that option is chosen", async () => {
    const onCompleteAnyway = vi.fn();
    const user = userEvent.setup();

    render(
      <DoneTransitionDialog
        open
        openItemCount={2}
        pending={false}
        onGoBack={vi.fn()}
        onCompleteAnyway={onCompleteAnyway}
        onSkipRemainingThenComplete={vi.fn()}
      />
    );

    await user.click(screen.getByRole("button", { name: "Complete anyway" }));
    expect(onCompleteAnyway).toHaveBeenCalled();
  });

  it("calls onSkipRemainingThenComplete when that option is chosen", async () => {
    const onSkip = vi.fn();
    const user = userEvent.setup();

    render(
      <DoneTransitionDialog
        open
        openItemCount={2}
        pending={false}
        onGoBack={vi.fn()}
        onCompleteAnyway={vi.fn()}
        onSkipRemainingThenComplete={onSkip}
      />
    );

    await user.click(screen.getByRole("button", { name: "Mark remaining as Skipped, then complete" }));
    expect(onSkip).toHaveBeenCalled();
  });

  it("renders nothing interactive when closed", () => {
    render(
      <DoneTransitionDialog
        open={false}
        openItemCount={2}
        pending={false}
        onGoBack={vi.fn()}
        onCompleteAnyway={vi.fn()}
        onSkipRemainingThenComplete={vi.fn()}
      />
    );

    expect(screen.queryByRole("button", { name: "Go back" })).not.toBeInTheDocument();
  });
});
