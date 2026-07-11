## MODIFIED Requirements

### Requirement: List household dashboards
The system SHALL provide `GET /api/v1/households/{householdId}/dashboards` (any Active member or PlatformAdmin) returning a lightweight list of all dashboards for the household. Each item includes `id`, `name`, `isDefault`, and `position`. If no dashboards exist, the system SHALL auto-create a default "Overview" dashboard before returning it, with widgets in this order: CylinderIndex (Small), TotalDisplacement (Small), TotalHorsepower (Small), AssetCount (Small), RecentActivity (Full, 5-item limit), DueSoon (Full, 30-day window).

#### Scenario: Member lists dashboards for a household with existing dashboards
- **WHEN** an Active member calls `GET /api/v1/households/{householdId}/dashboards`
- **THEN** HTTP 200 is returned with an array of dashboard summaries ordered by `position`

#### Scenario: First-time call auto-creates the default Overview dashboard
- **WHEN** a member calls `GET .../dashboards` for a household that has no dashboards yet
- **THEN** HTTP 200 is returned with one dashboard named "Overview" with `isDefault: true`

#### Scenario: Default Overview dashboard has the expected widget composition and order
- **WHEN** a member's first call to `GET .../dashboards` triggers auto-creation of the default dashboard
- **THEN** `GET .../dashboards/{id}` for that dashboard returns widgets in this order: `CylinderIndex`, `TotalDisplacement`, `TotalHorsepower`, `AssetCount`, `RecentActivity`, `DueSoon`

#### Scenario: Viewer can list dashboards
- **WHEN** a user with `Role = Viewer` calls the list dashboards endpoint
- **THEN** HTTP 200 is returned

#### Scenario: Non-member cannot list dashboards
- **WHEN** a user who is not an Active member of the household calls the list dashboards endpoint
- **THEN** HTTP 403 is returned
