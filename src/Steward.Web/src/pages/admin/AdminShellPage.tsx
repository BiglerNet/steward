import { NavLink, Outlet } from "react-router";
import { cn } from "@/lib/utils";

const ADMIN_SECTIONS = [{ to: "/admin/templates", label: "Templates" }];

export function AdminShellPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-h1">Admin</h1>
      <nav className="flex gap-0 overflow-x-auto border-b border-border">
        {ADMIN_SECTIONS.map((section) => (
          <NavLink
            key={section.to}
            to={section.to}
            className={({ isActive }) =>
              cn(
                "whitespace-nowrap border-b-2 border-transparent px-5 py-3 text-body text-muted-foreground transition-colors hover:text-foreground",
                isActive && "border-primary text-primary"
              )
            }
          >
            {section.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  );
}
