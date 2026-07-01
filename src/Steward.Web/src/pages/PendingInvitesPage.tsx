import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { acceptInvite } from "@/api/auth";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/context/AuthContext";

export function PendingInvitesPage() {
  const { pendingInvites, removePendingInvite } = useAuth();
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: (inviteCode: string) => acceptInvite(inviteCode),
    onSuccess: (_data, inviteCode) => {
      removePendingInvite(inviteCode);
      queryClient.invalidateQueries({ queryKey: ["households"] });
      toast.success("Invite accepted.");
    },
    onError: () => {
      toast.error("Couldn't accept that invite. It may have expired.");
    },
  });

  if (pendingInvites.length === 0) {
    return <p className="text-sm text-muted-foreground">You have no pending invites.</p>;
  }

  return (
    <div className="space-y-3">
      <h1 className="text-xl font-medium">Pending invites</h1>
      <ul className="space-y-2">
        {pendingInvites.map((invite) => (
          <li
            key={invite.inviteCode}
            className="flex items-center justify-between rounded-md border border-border p-3"
          >
            <div>
              <p className="font-medium">{invite.householdName}</p>
              <p className="text-sm text-muted-foreground">Role: {invite.role}</p>
            </div>
            <Button
              size="sm"
              disabled={mutation.isPending}
              onClick={() => mutation.mutate(invite.inviteCode)}
            >
              Accept
            </Button>
          </li>
        ))}
      </ul>
    </div>
  );
}
