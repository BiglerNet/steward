## 1. AuthService — Direct Registration Path (Infrastructure)

- [x] 1.1 In `AuthService.RegisterAsync`, after `userManager.CreateAsync` succeeds, read `configuration["PlatformAdmin:Email"]`; if non-empty and matches `request.Email` (case-insensitive), call `await userManager.AddToRoleAsync(user, PlatformAdminRoleSeeder.RoleName)`
- [x] 1.2 Verify: register with the configured email → JWT claims contain `PlatformAdmin`; register with a different email → JWT claims do not contain `PlatformAdmin`; empty config → no change to behavior

## 2. AuthService — OAuth Registration Path (Infrastructure)

- [x] 2.1 In `AuthService.HandleOAuthCallbackAsync`, inside the `user is null → CreateAsync` branch (new user only), apply the same `PlatformAdmin:Email` check and `AddToRoleAsync` call immediately after successful creation
- [x] 2.2 Verify the check is NOT present in the returning-user branch (above the new-user `if` block)

## 3. Config and Environment (Infrastructure + Helm)

- [x] 3.1 Add `PlatformAdmin:Email` to `appsettings.json` with a comment and empty default value so the key is documented for local dev
- [x] 3.2 Add `PLATFORMADMIN__EMAIL=` to `.env.example` with a comment explaining its purpose
- [x] 3.3 Add `platformAdmin.email: ""` to `charts/steward/values.yaml` (default empty — safe)
- [x] 3.4 Add `PlatformAdmin__Email` env var injection to the API Deployment template (`charts/steward/templates/api-deployment.yaml`), sourced from `{{ .Values.platformAdmin.email }}`
- [x] 3.5 Add the same env var to the setup Job template (`charts/steward/templates/setup-job.yaml`) so it is available if setup ever needs it

## 4. Unit Tests (UnitTests project)

- [x] 4.1 Add unit tests for `AuthService.RegisterAsync`: matching email → `AddToRoleAsync` called with `"PlatformAdmin"`; non-matching email → `AddToRoleAsync` not called; empty config → `AddToRoleAsync` not called
- [x] 4.2 Add unit tests for the OAuth path in `HandleOAuthCallbackAsync`: new user with matching email → role assigned; existing user with matching email → role not assigned
