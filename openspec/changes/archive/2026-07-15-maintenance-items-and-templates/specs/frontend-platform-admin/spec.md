## ADDED Requirements

### Requirement: Admin route guard
The frontend SHALL provide a `PlatformAdminRoute` guard component, structurally analogous to the existing `ProtectedRoute`, that checks the current user's JWT for the `PlatformAdmin` role claim and redirects (or shows a not-found/forbidden state) for any user lacking it, for every route nested under `/admin`.

#### Scenario: Non-admin is blocked from admin routes
- **WHEN** an authenticated user without the `PlatformAdmin` role navigates directly to `/admin/templates`
- **THEN** the app does not render the admin content and redirects or shows a forbidden state

#### Scenario: PlatformAdmin can access admin routes
- **WHEN** a user with the `PlatformAdmin` role navigates to `/admin/templates`
- **THEN** the admin content renders normally

---

### Requirement: Conditional admin nav link
The top navigation SHALL show an "Admin" link only when the current user's JWT carries the `PlatformAdmin` role; it SHALL be absent entirely (not merely disabled) for all other users.

#### Scenario: Admin link hidden for regular users
- **WHEN** a household Owner (without `PlatformAdmin`) views the top navigation
- **THEN** no "Admin" link is present anywhere in it

#### Scenario: Admin link shown for platform admins
- **WHEN** a `PlatformAdmin` user views the top navigation
- **THEN** an "Admin" link is present, linking to `/admin`

---

### Requirement: Admin shell
The frontend SHALL provide an `/admin` shell with its own sub-navigation, structured to accommodate additional admin sections in the future without restructuring. Platform template management is the first section, at `/admin/templates`.

#### Scenario: Admin shell hosts the templates section
- **WHEN** a PlatformAdmin navigates to `/admin`
- **THEN** the shell's sub-navigation includes a "Templates" entry linking to `/admin/templates`

---

### Requirement: Platform template management screen
`/admin/templates` SHALL list all platform templates with create/edit/delete controls (including step management: add/edit/delete/reorder steps, with engine-scoping and recurrence interval fields), calling the PlatformAdmin-only template endpoints.

#### Scenario: Admin creates a platform template
- **WHEN** a PlatformAdmin creates a new platform template with two steps via this screen
- **THEN** the template is created with `householdId: null` and is immediately visible to all households via the platform template catalog

#### Scenario: Admin edits a seeded built-in template
- **WHEN** a PlatformAdmin edits the seeded "Oil change" template's title
- **THEN** the change persists and is reflected in every household's view of the platform catalog going forward (existing duplicated household copies are unaffected)
