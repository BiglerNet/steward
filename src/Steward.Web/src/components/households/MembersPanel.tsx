import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { z } from "zod";
import { inviteMember, listMembers, removeMember, revokeInvitation } from "@/api/households";
import type { HouseholdMemberRole } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";

const inviteSchema = z.object({
  email: z.string().email("Enter a valid email address"),
  role: z.enum(["Contributor", "Viewer"]),
});

type InviteFormValues = z.infer<typeof inviteSchema>;

const ASSIGNABLE_ROLES: HouseholdMemberRole[] = ["Contributor", "Viewer"];

interface MembersPanelProps {
  householdId: string;
  canManage: boolean;
}

export function MembersPanel({ householdId, canManage }: MembersPanelProps) {
  const queryClient = useQueryClient();
  const { data } = useQuery({
    queryKey: ["households", householdId, "members"],
    queryFn: () => listMembers(householdId),
  });

  const form = useForm<InviteFormValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: { email: "", role: "Contributor" },
  });

  function invalidate() {
    queryClient.invalidateQueries({ queryKey: ["households", householdId, "members"] });
  }

  const inviteMutation = useMutation({
    mutationFn: (values: InviteFormValues) => inviteMember(householdId, values),
    onSuccess: () => {
      invalidate();
      form.reset();
      toast.success("Invite sent.");
    },
    onError: () => {
      toast.error("Couldn't send the invite.");
    },
  });

  const revokeMutation = useMutation({
    mutationFn: (code: string) => revokeInvitation(householdId, code),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't revoke the invite."),
  });

  const removeMutation = useMutation({
    mutationFn: (userId: string) => removeMember(householdId, userId),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't remove that member."),
  });

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-h3">Members</h2>
        <ul className="mt-2 space-y-2">
          {data?.members.map((member) => (
            <li
              key={member.userId}
              className="flex items-center justify-between rounded-md border border-border p-3"
            >
              <div>
                <p>{member.displayName ?? member.email}</p>
                <p className="text-sm text-muted-foreground">{member.role}</p>
              </div>
              {canManage && member.role !== "Owner" && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={removeMutation.isPending}
                  onClick={() => removeMutation.mutate(member.userId)}
                >
                  Remove
                </Button>
              )}
            </li>
          ))}
        </ul>
      </div>

      <div>
        <h2 className="text-h3">Pending invitations</h2>
        <ul className="mt-2 space-y-2">
          {data?.pendingInvites.map((invite) => (
            <li
              key={invite.id}
              className="flex items-center justify-between rounded-md border border-border p-3"
            >
              <div>
                <p>{invite.email}</p>
                <p className="text-sm text-muted-foreground">{invite.role}</p>
              </div>
              {canManage && (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={revokeMutation.isPending}
                  onClick={() => revokeMutation.mutate(invite.inviteCode)}
                >
                  Revoke
                </Button>
              )}
            </li>
          ))}
        </ul>
      </div>

      {canManage && (
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit((values) => inviteMutation.mutate(values))}
            className="flex items-end gap-3"
          >
            <FormField
              control={form.control}
              name="email"
              render={({ field }) => (
                <FormItem className="flex-1">
                  <FormLabel>Invite by email</FormLabel>
                  <FormControl>
                    <Input type="email" placeholder="member@example.com" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="role"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Role</FormLabel>
                  <FormControl>
                    <select
                      className="h-9 rounded-md border border-input bg-background px-3 text-sm"
                      {...field}
                    >
                      {ASSIGNABLE_ROLES.map((role) => (
                        <option key={role} value={role}>
                          {role}
                        </option>
                      ))}
                    </select>
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button type="submit" disabled={inviteMutation.isPending}>
              {inviteMutation.isPending ? "Sending…" : "Send invite"}
            </Button>
          </form>
        </Form>
      )}
    </div>
  );
}
