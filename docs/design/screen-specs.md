# Steward — Screen Implementation Specs

> Each screen maps to a real React/TypeScript implementation path. The prototype HTML validates the visual design; these specs tell Claude Code exactly what to build in the codebase.

---

## Shared Layout

### Shell — `src/components/layout/AppShell.tsx`
- **Top navigation bar** (56px height, white surface, bottom border 1px solid border)
- Props: `{ activeNav: string, onNavChange?: (nav: string) => void }`
- Contains: logo, nav links, avatar dropdown
- Wrapped around every authenticated page
- **shadcn/ui components used:** `NavigationMenu` for nav bar, `DropdownMenu` for avatar menu

### NavLinks — `src/components/layout/NavLinks.tsx`
```ts
interface NavLinksProps {
  active: string;
  householdId?: string;
}
```
- Links: Dashboard (`/`), My Gear (`/assets`), New Entry (`/entries/new`), My Household (`/households`), Settings (`/settings`)
- On mobile (<768px): collapse to icon-only horizontal bar with labels hidden
- Active indicator: bottom border 1.5px accent

### AvatarDropdown — `src/components/layout/AvatarDropdown.tsx`
- Displays user initials, background accent color
- Dropdown: Profile, Settings, Log out
- Uses shadcn `DropdownMenu` + `Avatar`

---

## Screen 1: Dashboard — `src/pages/DashboardPage.tsx`

### Route: `GET /`

### API endpoints consumed
```
GET /api/households/{householdId}/statistics
  → { totalAssets: number, hoursSaved: number, ytdCost: number, overdueCount: number }

GET /api/households/{householdId}/assets?limit=6
  → AssetSummary[] (name, type, year, lastServiceDate, assetType)

GET /api/households/{householdId}/upcoming-due?limit=5
  → { upcoming: DueItem[], overdue: DueItem[] }
```

### Components to build
```ts
// DashboardPage.tsx — page-level layout
interface DashboardPageProps {}

// StatCardGrid — 4-column grid of stat cards
const StatCardGrid = ({ stats }: { stats: { totalAssets: number; hoursSaved: number; ytdCost: number; overdueCount: number } })

// StatCard — individual stat
interface StatCardProps {
  value: number;
  label: string;
  color?: 'accent' | 'success' | 'warn' | 'danger';
  suffix?: string;
}

// HouseholdSelector — dropdown to switch households
interface HouseholdSelectorProps {
  households: HouseholdSummary[];
  selectedId: string;
  onSelect: (id: string) => void;
  onCreate: () => void;
}

// AssetGrid — card grid showing "My Gear"
interface AssetGridProps {
  assets: AssetSummary[];
  onAdd: () => void;
}

// DueList — upcoming due items
interface DueListProps {
  items: DueItem[];
}
```

### Data binding notes
- **stat-value**: raw number from API, no prefix (not "$0" — just the number). Format ytdCost with currency (Intl.DateTimeFormat for locale-aware).
- **asset cards**: show name, asset type, year (from asset summary API response). Last service date shown as relative ("30 days ago") or "No service history" if null.
- **due items**: color-coded by status — overdue (red), due soon (amber), upcoming (green). Name/type from API DTOs.
- If totalAssets === 0, show empty-state CTA "Your garage is empty. Add your first asset →" linking to `/assets/new`.
- If no upcoming-due items, show "All clear — nothing due in the next 30 days."

### shadcn/ui components
- `Card` for stat cards, `DropdownMenu` for household selector, `Button` for Add Asset

---

## Screen 2: My Gear (Asset List) — `src/pages/AssetsPage.tsx`

### Route: `GET /assets`

### API endpoints consumed
```
GET /api/households/{householdId}/assets?filter={type?}&search={q?}
  → AssetSummary[] (id, name, type, year, vinOrHin, engineCount, registrationCount, lastServiceDate, status)
```

### Components to build
```ts
interface AssetsPageProps {
  householdId: string;
}

// AssetFilterBar — horizontal scrollable chip filter
interface AssetFilterBarProps {
  selected: AssetType | 'all';
  onSelect: (type: AssetType | 'all') => void;
  counts: Record<AssetType, number>;
}

// AssetsGrid — responsive grid of asset cards
interface AssetsGridProps {
  assets: AssetSummary[];
  view: 'grid' | 'list';
  onAddAsset: () => void;
}

// AssetCard — individual card
interface AssetCardProps {
  asset: AssetSummary;
}
```

### Asset types (10 options from OpenAPI spec)
```ts
type AssetType = 
  | 'Boat'
  | 'Car'
  | 'Truck'
  | 'Snowmobile'
  | 'Utv'
  | 'SnowmobileTrailer'
  | 'EnclosedTrailer'
  | 'RidingMower'
  | 'PowerWasher'
  | 'SmallEngine';
```

### Data binding notes
- Card shows: asset name, type, year, vin/hin, engine count, last maintenance action
- Footer shows status indicator: Active (green), Maintenance due (amber), Inactive (gray)
- Status derived from: overdue registrations, last service date > 180 days, warranty expired
- "Add Asset" card at end of grid (dashed border, + icon, links to `/assets/new`)
- Filter chips: "All (N)" + each asset type with count, active chip has green background

### shadcn/ui components
- `Card`, `Button` (outline variant for Add Asset), `Chip` (or custom for asset type filter), `Input` (for search)

---

## Screen 3: Asset Detail — `src/pages/AssetDetailPage.tsx`

### Route: `GET /assets/:id`

### API endpoints consumed
```
GET /api/assets/{id}
  → AssetDetail (full DTO: type, year, vin/hin, length, capacity, engines[], etc. — type-specific fields)

GET /api/assets/{id}/service-records
  → ServiceRecord[] { date, description, provider, cost, notes, engineId? }

GET /api/assets/{id}/fuel-logs
  → FuelLog[] { date, logType, volume, volumeUnit, pricePerUnit, totalCost, fuelGrade, notes }

GET /api/assets/{id}/mileage-logs
  → MileageLog[] { date, reading, purpose, notes }

GET /api/assets/{id}/engine-hours
  → EngineHourLog[] { date, hours, readingType (total/trip), engineId, notes }

GET /api/assets/{id}/engines
  → Engine[] { id, name, manufacturer, model, hours, status (active/retired), createdAt, retiredAt? }

GET /api/assets/{id}/registrations
  → Registration[] { id, authority, number, issueDate, expiryDate, cost }

GET /api/assets/{id}/warranties
  → Warranty[] { id, provider, startDate, endDate, coverage, cost, docUrl? }

GET /api/assets/{id}/documents
  → DocumentSummary[] { id, filename, mimeType, uploadDate, size }
```

### Components to build
```ts
interface AssetDetailPageProps {
  householdId: string;
  assetId: string;
}

// AssetHeader — breadcrumb + asset info + action buttons
interface AssetHeaderProps {
  asset: AssetDetail;
}

// AssetDetailGrid — two-column grid of detail panels
interface AssetDetailGridProps {
  asset: AssetDetail;
  type: AssetType;
}

// DetailCard — reusable panel within detail grid
interface DetailCardProps {
  title: string;
  children: ReactNode;
  actions?: ReactNode;
}

// RegWarrantyCard — registration or warranty card
interface RegWarrantyCardProps {
  item: Registration | Warranty;
  type: 'registration' | 'warranty';
  onRenew?: () => void;
  onViewDoc?: () => void;
}

// TrackingRecordTable — filterable table of records by type
interface TrackingRecordTableProps {
  records: ServiceRecord[] | FuelLog[] | MileageLog[] | EngineHourLog[];
  recordType: 'service' | 'fuel' | 'mileage' | 'hours';
  onAdd?: () => void;
}

// EngineList — list of engines on this asset
interface EngineListProps {
  engines: Engine[];
  onAdd?: () => void;
  onEdit?: (engineId: string) => void;
  onRetire?: (engineId: string) => void;
}

// Tabs — type-specific tracking tabs
interface AssetTabsProps {
  assetId: string;
}
```

### Tab structure (6 tabs, each with count badge)
1. **Service** — count from API (service-records.length)
2. **Fuel** — count from API (fuel-logs.length)
3. **Mileage** — count from API (mileage-logs.length)
4. **Hours** — count from API (engine-hours.length)
5. **Engines** — count from API (engines.length)
6. **Docs** — count from API (documents.length)

### Data binding notes
- Asset detail card: type-specific fields from OpenAPI spec (e.g., boat has length/capacity/engine, car has vin/make/model)
- Upcoming renewals: derived from registrations/warranties where expiryDate <= 90 days from now
- Registrations: expiry date, cost, authority, number. Status badge: valid (green) / expired (red).
- Warranties: provider, coverage, cost, expiry date. Status badge: valid / expired.
- Engines: status dot (active=green, retired=gray), name, manufacturer, model
- Service/fuel/mileage/hours: paginated table with date as primary sort (desc)
- Cost columns: tabular-nums, formatted with currency
- "Log Service" button: primary action, links to `/assets/:id/entries/new?recordType=service`

### shadcn/ui components
- `Card`, `Table`, `Tabs`, `TabsContent`, `TabsList`, `TabsTrigger`, `Button`, `Badge`, `DropdownMenu`, `Dialog` (for edit/add modals)

---

## Screen 4: New Entry (Tracking Record) — `src/pages/NewEntryPage.tsx`

### Route: `GET /entries/new`

### API endpoints consumed
```
GET /api/assets/{assetId}               → AssetSummary (pre-filled asset selector)
POST /api/assets/{assetId}/service-records → create service record
POST /api/assets/{assetId}/fuel-logs     → create fuel log
POST /api/assets/{assetId}/mileage-logs  → create mileage log
POST /api/assets/{assetId}/engine-hours  → create engine hour log
```

### Components to build
```ts
interface NewEntryPageProps {
  householdId: string;
}

// RecordTypeSelector — 2x2 grid type selection cards
interface RecordTypeSelectorProps {
  selected: RecordType;
  onSelect: (type: RecordType) => void;
}

// ServiceForm — service record form fields
interface ServiceFormProps {
  assetId: string;
  onSubmit: (data: ServiceRecordInput) => void;
  onCancel: () => void;
}

// FuelForm — fuel log form fields
interface FuelFormProps {
  assetId: string;
  onSubmit: (data: FuelLogInput) => void;
  onCancel: () => void;
}

// MileageForm — mileage log form fields
interface MileageFormProps {
  assetId: string;
  onSubmit: (data: MileageLogInput) => void;
  onCancel: () => void;
}

// EngineHoursForm — engine hours form fields
interface EngineHoursFormProps {
  assetId: string;
  onSubmit: (data: EngineHourInput) => void;
  onCancel: () => void;
}
```

### Record types (4 options)
```ts
type RecordType = 'service' | 'fuel' | 'mileage' | 'hours';
```

### Form field specs (from OpenAPI)

**Service Record**
```ts
interface ServiceRecordInput {
  date: string;             // required, YYYY-MM-DD
  description: string;      // required
  cost: number;             // required
  serviceType?: string;     // optional (oil-change, engine-repair, winterization, etc.)
  providerName?: string;    // optional
  engineId?: string;        // optional, links to specific engine
  odometerHours?: number;   // optional, current reading
  notes?: string;           // optional
}
```

**Fuel Log**
```ts
interface FuelLogInput {
  date: string;             // required
  logType: 'fillup' | 'consumption';  // required
  volume: number;           // required
  volumeUnit: 'gallons' | 'liters';   // required
  pricePerUnit?: number;    // optional
  totalCost?: number;       // optional, auto-calculated = volume × pricePerUnit
  fuelGrade?: string;       // optional
  engineId?: string;        // optional
  milesAtLog?: number;      // optional
  hoursAtLog?: number;      // optional
  notes?: string;           // optional
}
```

**Mileage Log**
```ts
interface MileageLogInput {
  date: string;             // required
  logType: 'odometer' | 'trip-miles'; // required (switches form label)
  reading: number;          // required, differs by logType
  purpose?: string;         // optional (personal, business, charity, commute)
  notes?: string;           // optional
}
```

**Engine Hours Log**
```ts
interface EngineHourInput {
  date: string;             // required
  readingType: 'total' | 'trip';  // required, radio toggle
  hours: number;            // required
  engineId: string;         // required
  notes?: string;           // optional
}
```

### Data binding notes
- Asset selector at top (dropdown or search) — defaults to asset passed in URL query (`?assetId=...`)
- Type selection cards shown as 2x2 grid, selected card has green border + background + shadow
- Each record type has its own form variant (only one visible at a time)
- Required fields marked with red asterisk
- Optional fields toggleable via "Add optional fields" link (collapsible)
- On submit: POST to appropriate endpoint, show success snackbar, redirect to asset detail page
- On cancel: go back to previous page or asset detail
- Form validation: required fields, numeric ranges, date format. Show inline error messages below fields.

### shadcn/ui components
- `Card`, `Form`, `Input`, `Select`, `Textarea`, `Button`, `RadioGroup`, `Label`, `Separator`, `Alert` (for validation errors)

---

## Screen 5: Household Settings — `src/pages/HouseholdSettingsPage.tsx`

### Route: `GET /settings`

### API endpoints consumed
```
GET /api/households/{householdId}
  → { id, name, members: HouseholdMember[], invites: InviteSummary[] }

GET /api/users/me
  → { id, name, email }

PUT /api/households/{householdId}
  → { name: string }

POST /api/households/{householdId}/invites
  → { email, status, role }

PATCH /api/households/{householdId}/invites/{inviteId}/revoke
  → 204

DELETE /api/households/{householdId}
  → 204

PUT /api/households/{householdId}/members/{memberId}/role
  → { role: 'owner' | 'contributor' | 'viewer' }

PATCH /api/users/me
  → updated user profile
```

### Components to build
```ts
interface HouseholdSettingsPageProps {
  householdId: string;
}

// ProfileSection — user profile card
interface ProfileSectionProps {
  user: UserProfile;
  onEditProfile: () => void;
}

// HouseholdNameSection — rename household card
interface HouseholdNameSectionProps {
  currentName: string;
  onSave: (name: string) => void;
}

// MembersSection — member list + invite
interface MembersSectionProps {
  members: HouseholdMember[];
  invites: InviteSummary[];
  onInvite: (email: string, role: Role) => void;
  onRevokeInvite: (inviteId: string) => void;
  onChangeRole: (memberId: string, role: Role) => void;
  onRemoveMember: (memberId: string) => void;
}

// DangerZoneSection — delete household confirmation
interface DangerZoneSectionProps {
  onDelete: () => void;
}
```

### Role labels
```ts
type Role = 'owner' | 'contributor' | 'viewer';
```

### Data binding notes
- **Profile**: avatar (initials from name), email, "Edit Profile" buttons links to settings/profile
- **Household name**: shows current name, editable inline with Save/Cancel
- **Members**: list with avatar, name, email, role badge. Each row has 3-dot menu (Edit role, Remove)
- **Pending invites**: displayed as yellow-backed cards. Show sent date + role. Revoke button (destructive)
- **Invite new member**: email input + role dropdown (Contributor/Viewer) + Invite button
- **Delete household**: red-bordered card at bottom with warning text. Shows confirmation dialog before deletion

### shadcn/ui components
- `Card`, `Avatar`, `Table`, `Input`, `Select`, `DropdownMenu`, `Dialog` (for edit/remove confirmations), `Button` (destructive variant)

---

## Screen 6: Login/Register — `src/pages/AuthPage.tsx`

### Route: `GET /auth`

### API endpoints consumed
```
POST /api/auth/register
  → { token, user: { name, email } }

POST /api/auth/login
  → { token, user: { name, email } }

POST /api/auth/oauth/google
  → redirect URL

POST /api/auth/oauth/facebook
  → redirect URL

POST /api/auth/oauth/apple
  → redirect URL
```

### Components to build
```ts
interface AuthPageProps {
  onSuccess: (token: string, user: UserProfile) => void;
}

const AuthPage = ({ onSuccess }) => {
  // Tab state: 'login' | 'register'
  // Form state for email/password/name
  // OAuth button handlers
}
```

### Data binding notes
- Tabs switch between Log in / Register forms
- Error message shown at top of form on invalid credentials (red bordered card with icon)
- OAuth buttons: Google (G logo), Facebook (blue F), Apple (black apple icon)
- Password strength indicator on register (8+ character requirement)
- On success: store token in auth context, redirect to dashboard
- On OAuth redirect: window.location = oauthRedirectUrl

### shadcn/ui components
- `Card`, `Tabs`, `TabsContent`, `TabsList`, `TabsTrigger`, `Input`, `Label`, `Button`, `Separator` (for divider "or continue with")

---

## Screen 7: Create Household — `src/pages/CreateHouseholdPage.tsx`

### Route: `GET /households/new`

### API endpoints consumed
```
POST /api/households
  → { id, name, ownerUser: UserProfile, members: [UserProfile] }
```

### Components to build
```ts
interface CreateHouseholdPageProps {
  onSuccess: (householdId: string) => void;
}
```

### Data binding notes
- Centered card with "Create a household" heading
- Single input: household name (required)
- Optional description/notes field
- On submit: creates household, redirects to dashboard
- Empty state (if user has no household): show this page automatically after login

### shadcn/ui components
- `Card`, `Input`, `Label`, `Button`

---

## Screen 8: My Household (Overview) — `src/pages/HouseholdOverviewPage.tsx`

### Route: `GET /households`

### API endpoints consumed
```
GET /api/households                              → HouseholdSummary[] (list all user's households)
GET /api/households/{householdId}                → HouseholdDetail (full)
GET /api/households/{householdId}/members        → HouseholdMember[]
GET /api/households/{householdId}/invites        → InviteSummary[]
```

### Components to build
```ts
interface HouseholdOverviewPageProps {
  households: HouseholdSummary[];
}
```

### Data binding notes
- Shows all households user belongs to (cards or list)
- Each card: household name, member count, asset count, current toggle indicator
- "Create new household" card at end
- Clicking a card navigates to that household's dashboard

### shadcn/ui components
- `Card`, `Button`

---

## Shared Form Components

### TextField — `src/components/form/TextField.tsx`
- Input with label, placeholder, error state, hint text
- Uses shadcn `Input` internally
- Props: `label`, `placeholder`, `type` (text/email/number/date/password), `error`, `hint`, `required`

### SelectField — `src/components/form/SelectField.tsx`
- Select with label, options, error state
- Props: `label`, `options`, `value`, `onChange`, `error`, `placeholder`

### TextAreaField — `src/components/form/TextAreaField.tsx`
- Textarea with label, error state, hint text
- Props: `label`, `placeholder`, `value`, `onChange`, `error`, `hint`, `minHeight`, `label`

### NumberField — `src/components/form/NumberField.tsx`
- Number input with prefix label (e.g., "$" or "gal")
- Props: `label`, `placeholder`, `value`, `onChange`, `error`, `prefix`, `suffix`, `step`

### DateField — `src/components/form/DateField.tsx`
- Date picker input with label
- Props: `label`, `value`, `onChange`, `error`, `min`, `max`, `placeholder`

### ToggleOptional — `src/components/form/ToggleOptional.tsx`
- Collapsible "Optional fields" toggle with chevron icon
- Props: `children`, `label` (default: "Additional details")

---

## Shared Component Library

### Button — `src/components/ui/Button.tsx`
```ts
type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'destructive' | 'ghost' | 'link';
interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: 'sm' | 'md' | 'lg';
}
```

### StatusBadge — `src/components/ui/StatusBadge.tsx`
```ts
type StatusVariant = 'active' | 'valid' | 'upcoming' | 'warn' | 'overdue' | 'expired' | 'retired' | 'inactive';
interface StatusBadgeProps {
  status: StatusVariant;
  children: ReactNode;
}
```
- Colors mapping:
  - active/valid/upcoming → green (bg: #d1fae5, text: #059669)
  - warn → amber (bg: #fef3c7, text: #d97706)
  - overdue/expired → red (bg: #fef2f2, text: #dc2626)
  - retired/inactive → gray (bg: #f1f5f9, text: #64748b)

### CurrencyDisplay — `src/components/ui/CurrencyDisplay.tsx`
```ts
interface CurrencyDisplayProps {
  value: number;
  locale?: string;
  showPrefix?: boolean;
}
```

### EmptyState — `src/components/ui/EmptyState.tsx`
```ts
interface EmptyStateProps {
  title: string;
  description: string;
  action?: { label: string; onClick: () => void };
}
```

### FileUploader — `src/components/ui/FileUploader.tsx`
```ts
interface FileUploaderProps {
  accept?: string;
  maxSizeMB?: number;
  onUpload: (file: File) => Promise<void>;
  onRemove: (fileId: string) => void;
}
```
