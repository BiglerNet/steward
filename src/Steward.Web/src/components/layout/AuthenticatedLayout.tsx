import { Wrench, Package, Settings, LayoutDashboard } from "lucide-react";
import { Link, NavLink, Outlet, useParams } from "react-router";
import { PendingInvitesBanner } from "@/components/auth/PendingInvitesBanner";
import { UserMenu } from "@/components/auth/UserMenu";
import { HouseholdSwitcher } from "@/components/households/HouseholdSwitcher";
import { cn } from "@/lib/utils";

export function AuthenticatedLayout() {
  const { householdId } = useParams();

  const navLinks = householdId
    ? [
        { to: `/households/${householdId}`, label: "Dashboard", icon: LayoutDashboard },
        { to: `/households/${householdId}/assets`, label: "My Equipment", icon: Package },
        { to: `/households/${householdId}/settings`, label: "Household Settings", icon: Settings },
      ]
    : [];

  return (
    <div className="flex min-h-svh flex-col bg-background">
      <PendingInvitesBanner />
      <header className="flex h-14 items-center justify-between border-b border-border bg-card px-4 sm:px-8">
        <div className="flex items-center gap-6">
          <Link
            to="/households"
            className="flex items-center gap-2 text-h3 font-semibold tracking-tight"
          >
            <Wrench className="h-5 w-5 text-primary" />
            <span className="hidden md:inline">Steward</span>
          </Link>
          <HouseholdSwitcher />
          {navLinks.length > 0 && (
            <nav className="flex h-14 items-center gap-1">
              {navLinks.map(({ to, label, icon: Icon }) => (
                <NavLink
                  key={to}
                  to={to}
                  className={({ isActive }) =>
                    cn(
                      "flex h-14 items-center gap-2 border-b-[1.5px] border-transparent px-3 text-body text-muted-foreground transition-colors hover:text-foreground",
                      isActive && "border-primary text-primary"
                    )
                  }
                >
                  <Icon className="h-4 w-4 md:hidden" />
                  <span className="hidden md:inline">{label}</span>
                </NavLink>
              ))}
            </nav>
          )}
        </div>
        <UserMenu />
      </header>
      <main className="flex-1 p-4 sm:p-8">
        <div className="mx-auto w-full max-w-[1200px]">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
