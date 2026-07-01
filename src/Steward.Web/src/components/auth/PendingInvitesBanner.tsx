import { Link } from "react-router";
import { useAuth } from "@/context/AuthContext";

export function PendingInvitesBanner() {
  const { pendingInvites } = useAuth();

  if (pendingInvites.length === 0) {
    return null;
  }

  return (
    <div className="flex items-center justify-between bg-accent px-4 py-2 text-sm text-accent-foreground">
      <span>
        You have {pendingInvites.length} pending household invite
        {pendingInvites.length === 1 ? "" : "s"}.
      </span>
      <Link to="/invites" className="font-medium underline underline-offset-2">
        View invites
      </Link>
    </div>
  );
}
