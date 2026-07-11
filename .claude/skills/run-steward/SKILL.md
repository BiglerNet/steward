---
name: run-steward
description: Launch and drive the Steward app (API + web) for a manual/browser smoke check. Use when asked to run, start, or screenshot the app, or to confirm a change works end-to-end rather than just in unit/integration tests.
---

Two ways to run the stack. Default to **Option A** for smoke-testing a code change
(fast, hot-reload, matches how you've been developing). Use **Option B** only when
the check specifically needs container parity (e.g. verifying the Dockerfile/deploy
config itself, or testing the Nginx-served frontend build).

## Option A: native API + native web (default)

Postgres runs in Docker; API and web run as host processes.

```bash
docker compose up -d postgres
timeout 30 bash -c 'until docker compose exec -T postgres pg_isready -U steward >/dev/null 2>&1; do sleep 1; done'

# launchSettings.json's default port (5000) already matches the frontend's
# src/Steward.Web/.env.development VITE_API_BASE_URL — no --urls override needed.
dotnet run --project src/Steward.Api &
timeout 60 bash -c 'until curl -sf http://localhost:5000/healthz >/dev/null 2>&1; do sleep 2; done'

cd src/Steward.Web && npm run dev &
timeout 30 bash -c 'until curl -sf http://localhost:5173 >/dev/null 2>&1; do sleep 1; done'
```

App is at `http://localhost:5173`. API docs (Scalar) at `http://localhost:5000/scalar/v1`.

**Stop cleanly** — don't leave orphans, they'll squat the ports next run:
```bash
netstat -ano | grep -E ':5000|:5173' | grep LISTENING   # find PIDs
taskkill //F //PID <pid>                                 # repeat per PID
```
(Bash job-control `kill %1` doesn't reliably reach `dotnet run`'s child process on
Windows — killing by PID via `netstat` is what actually works.)

If port 5000 (or 5173) is already bound from a previous session, that's a leftover —
find and kill it the same way before starting a fresh one, don't just pick a new port.

## Option B: fully containerized (container parity)

```bash
cp .env.example .env   # only if .env doesn't already exist — check first, don't clobber
docker compose up --build
```

- Web (Nginx-served build): `http://localhost:8081`
- API: `http://localhost:5000`
- First run against a fresh Postgres volume needs migrations applied from the host:
  ```bash
  dotnet ef database update --project src/Steward.Infrastructure --startup-project src/Steward.Api \
    --connection "Host=localhost;Port=5432;Database=steward;Username=steward;Password=steward"
  docker compose up -d api   # restart so it starts cleanly against the migrated schema
  ```
- Requires `BIGLERNET_NPM_TOKEN` (GitHub PAT, `read:packages`) in `.env` to build the
  `web` image — it installs `@biglernet/*` from GitHub Packages. Without it, `web`'s
  build fails; fall back to Option A for the frontend in that case.
- Code changes need a rebuild: `docker compose up --build api web`. No hot-reload.

Stop: `docker compose down` (add `-v` only if you intend to wipe the Postgres volume —
ask first, that's destructive).

## Driving it

No `chromium-cli` in this environment. For a browser smoke check, use Playwright
installed standalone in the scratchpad (don't add it to the repo's `package.json`
unless asked):

```bash
mkdir -p "$SCRATCHPAD/pw-smoke" && cd "$SCRATCHPAD/pw-smoke"
npm init -y && npm install playwright@1.61.1 && npx playwright install chromium
```

Then a Node script using `require('playwright').chromium.launch()` — `page.goto('http://localhost:5173/...')`,
interact, `page.screenshot(...)`, and check `console --errors` equivalent by listening
for `page.on('console', ...)` / `page.on('pageerror', ...)`.

Gotcha: shadcn/Radix `Select` popovers can be flaky to open back-to-back with plain
`.click()` on the option — if a click on `[role="option"]` hangs, prefer keyboard
selection (click the trigger, then `ArrowDown` × N + `Enter`) over chasing the popover
with more clicks/waits.

Auth: register a fresh test user through the UI (`/register` — Display name, Email,
Password, Confirm password) and create a household through the UI
(`/households` → "Create household" → Household name). There's no seeded test account
or API shortcut for this currently.
