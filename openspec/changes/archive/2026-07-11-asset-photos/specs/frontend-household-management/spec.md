## ADDED Requirements

### Requirement: Storage usage display
The frontend SHALL show the household's storage consumption on the household settings page — used bytes against the effective quota from the household detail response, rendered as a human-readable summary (e.g. "412 MB of 1 GB") with a progress indicator. The display SHALL be visible to all members and SHALL offer no controls to change the quota.

#### Scenario: Member sees storage usage
- **WHEN** any household member opens the household settings page
- **THEN** the current usage and effective quota are shown with a progress indicator

#### Scenario: Near-full households are highlighted
- **WHEN** usage exceeds 90% of the effective quota
- **THEN** the progress indicator switches to a warning treatment
