import { ChevronDown, Plus } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { CreateHouseholdDialog } from "@/components/households/CreateHouseholdDialog";
import { useHouseholds } from "@/hooks/useHouseholds";
import { writeLastHouseholdId } from "@/lib/session";

function buildSwitchPath(
  pathname: string,
  currentHouseholdId: string | undefined,
  newHouseholdId: string
): string {
  if (!currentHouseholdId) {
    return `/households/${newHouseholdId}`;
  }
  return pathname.replace(`/households/${currentHouseholdId}`, `/households/${newHouseholdId}`);
}

export function HouseholdSwitcher() {
  const { data: households } = useHouseholds();
  const { householdId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  const current = households?.find((household) => household.id === householdId);

  if (!households || households.length === 0) {
    return null;
  }

  function handleSelect(newHouseholdId: string) {
    if (newHouseholdId === householdId) {
      return;
    }
    writeLastHouseholdId(newHouseholdId);
    navigate(buildSwitchPath(location.pathname, householdId, newHouseholdId));
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" className="gap-2">
          {current?.name ?? "Select household"}
          <ChevronDown className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start">
        <DropdownMenuLabel>Households</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {households.map((household) => (
          <DropdownMenuItem key={household.id} onSelect={() => handleSelect(household.id)}>
            {household.name}
          </DropdownMenuItem>
        ))}
        <DropdownMenuSeparator />
        <CreateHouseholdDialog
          trigger={
            <DropdownMenuItem onSelect={(event) => event.preventDefault()}>
              <Plus className="mr-2 h-4 w-4" />
              Create household
            </DropdownMenuItem>
          }
        />
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
