import { useParams } from "react-router";
import { useHouseholds } from "@/hooks/useHouseholds";
import type { HouseholdMemberRole } from "@/api/types";

export interface HouseholdRolePermissions {
  role: HouseholdMemberRole | undefined;
  canEdit: boolean;
  canDeleteStructural: boolean;
}

export function canEditWithRole(role: HouseholdMemberRole | undefined): boolean {
  return role === "Contributor" || role === "Owner";
}

export function canDeleteStructuralWithRole(role: HouseholdMemberRole | undefined): boolean {
  return role === "Owner";
}

export function useHouseholdRole(): HouseholdRolePermissions {
  const { householdId } = useParams();
  const { data: households } = useHouseholds();
  const role = households?.find((household) => household.id === householdId)?.userRole;

  return {
    role,
    canEdit: canEditWithRole(role),
    canDeleteStructural: canDeleteStructuralWithRole(role),
  };
}
