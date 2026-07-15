# FinalCapstone — Mug Club Tracker

## What this app actually is

This project digitizes a tavern's "200 club": a customer who drinks all ~200 beers on the
tavern's list earns a mug. Today that's tracked on a paper sheet the bartender initials as
each beer is drunk. **The core product driver is the bartender-confirmed progress flow, not
generic beer-catalog CRUD** — customers can't self-report a beer as drunk; a bartender has to
confirm it, same as the paper initials. Catalog browsing/search/CRUD exists to support that
flow, not the other way around. See `PROJECT_PLAN.md` section 1 for the full framing.

## Repo layout — which code is active

- **`beer-app/`** — the active, in-progress refactor. This is where new work happens.
  - `backend/` — ASP.NET Core 8 Web API, EF Core 8 + Npgsql (PostgreSQL), ASP.NET Core
    Identity, JWT bearer auth, Swashbuckle/Swagger.
  - `BeerApi.Tests/` — xUnit test project (unit tests against EF Core's InMemory provider,
    plus `WebApplicationFactory<Program>` integration tests covering role-based authorization).
  - `frontend/` — React 18 + Vite 5 + react-router-dom 6, inline styles (no UI framework).
    Vitest + React Testing Library for tests, colocated as `*.test.jsx`/`*.test.js`.
  - `docker-compose.yml` — `db` (postgres:16-alpine), `api`, `web` services.
  - `infra/aws-architecture.md` — deployment design doc only, no actual IaC yet.
- **`BeerList/`** — legacy pre-refactor ASP.NET MVC 5 / EF6 app. Kept as historical
  reference only; not where new work happens. Notably it had `[Authorize(Roles = "canEdit")]`
  role gating that the new `beer-app` backend does not currently have.
- **Planning/vision docs** (root level, flat, no `docs/` folder): `PROJECT_PLAN.md`,
  `TECHNICAL_ARCHITECTURE_PLAN.md`, `FEATURE_MAP.md`, `IMPLEMENTATION_BACKLOG.md`,
  `MVP_SCREEN_PLAN.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`, `PRODUCT_FLOW_DIAGRAM.md`,
  and `PERSONAS_AND_USAGE.md` (persona day-in-the-life deep dive, added July 2026).
  These describe the target product and haven't gone stale the way a status tracker does.
  **Load-bearing July 2026 product decision — the one-device rule:** the entire at-the-bar
  flow lives on the *customer's* phone; the bartender confirms by typing their personal
  6-digit PIN on it. No bartender-facing screens, no bar tablet; the earlier "I'm drinking
  this" request-queue and QR-membership-card ideas are superseded — don't reintroduce them.
  See `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1.
- **`DIAGRAMS_AND_STORYBOARD.html`** — self-contained visual system architecture diagram, data
  model, bartender-confirmation core loop, and screen-by-screen storyboard. Open directly in a
  browser. Built/Sprint 1/Sprint 2 status is color-coded per element and should be re-checked
  against `EPICS_AND_SPRINTS.md` as sprints close, since it's a snapshot, not a live view.
- **Agile tracking** (current source of truth for status): `EPICS_AND_SPRINTS.md` (epics,
  sprints, links to GitHub Issues/Milestones) and `SESSION_LOG.md` (dated per-session record).
  `PROGRESS_TRACKER.md` and `PHASE1_IMPLEMENTATION_CHECKLIST.md` are retired stubs pointing here
  — they went stale (described a plain CRUD app, or stayed fully unchecked after the work was
  actually done) and were replaced rather than fixed in place.

Suggested reading order for onboarding: `PROJECT_PLAN.md` → `EPICS_AND_SPRINTS.md` for current
status/what's next → `FEATURE_MAP.md` / `IMPLEMENTATION_BACKLOG.md` for backlog detail →
`beer-app/README.md` for run instructions.

## Current implementation status (verified against code, not docs)

**Built:**
- `Beer` model — `Id, Name, Brewery, Style, Description, CreatedAt`
  (`beer-app/backend/Models/Beer.cs`)
- `BeersController` — GET all / GET by id (anonymous), POST/PUT/DELETE
  (`[Authorize(Roles = "Admin")]`) (`beer-app/backend/Controllers/BeersController.cs`)
- `AuthController` — `/api/auth/register` (assigns the `Customer` role) and `/api/auth/login`,
  JWTs via `IdentityUser` with role claims (`beer-app/backend/Controllers/AuthController.cs`)
- React pages: beer list, beer detail, create/edit form, login/register
  (`beer-app/frontend/src/pages/`)
- Docker Compose wiring for db/api/web
- EF Core migrations (`beer-app/backend/Migrations/`) and startup seeding of the
  `Admin`/`Bartender`/`Customer` roles plus sample beers (`beer-app/backend/Data/SeedData.cs`)
- **Note:** the auth/roles/migrations/seed work above is merged to `master` (via `harden-foundation`,
  [PR #7](https://github.com/pmconnolly80/FinalCapstone/pull/7)).
- **Sprint 1: Mug Club Core** (2026-07-14, [PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11)
  — merged to `master`; issues #2–#6 closed, milestone closed):
  - `Tavern`, `BeerConfirmation` (unique per customer+beer), `StaffPin` entities +
    `AddMugClubCore` migration (`beer-app/backend/Models/`, `Migrations/`)
  - `POST /api/confirmations {beerId, pin}` — authenticated as the *customer*, bartender
    resolved server-side from their hashed 6-digit PIN
    (`beer-app/backend/Controllers/ConfirmationsController.cs`)
  - `GET /api/me/progress` (`beer-app/backend/Controllers/MeController.cs`)
  - Confirmation PIN Pad (`beer-app/frontend/src/components/ConfirmPinPad.jsx`, launched
    from beer detail) and My Progress page (`beer-app/frontend/src/pages/MyProgress.jsx`,
    route `/progress`)
  - Seed adds "The Tavern" + a dev bartender: `bartender@example.com` / `Bartender1!`,
    PIN `123456` (dev bootstrap only — real PIN lifecycle is Sprint 2 scope)

**Not built** — next up per `EPICS_AND_SPRINTS.md`:
- Sprint 2 (Mug Club Completion, groomed 2026-07-14 into issues #12–#16,
  [milestone 2](https://github.com/pmconnolly80/FinalCapstone/milestone/2)): PIN lockout
  (#12 — `StaffPin` already has `FailedAttempts`/`LockedUntil` columns, unused), PIN
  lifecycle (#13), durable "mug earned" milestone (#14), admin confirmation
  audit/correction API + screen (#15/#16)
- No admin UI to assign roles (currently DB-manual only)
- No Open Brewery DB API integration — scoped in the 2026-07-13 planning session: OBDB is
  breweries-only (no beer-level endpoint), so it enriches beer details with brewery info and
  powers admin brewery autocomplete; the tavern's list stays the source of truth for beers.
  Catalog.beer researched 2026-07-14 as the beer-level pre-fill candidate (hit-rate spike
  first). See `TECHNICAL_ARCHITECTURE_PLAN.md` §6.

## Testing policy (TDD)

This is a TDD project: every new feature/story needs tests — unit and/or integration —
before it's considered done, not backfilled after. See `EPICS_AND_SPRINTS.md`'s
"Definition of Done" for the process rule.

- **Backend**: `beer-app/BeerApi.Tests` (xUnit). Controller-level CRUD logic is unit-tested
  against EF Core's InMemory provider; `[Authorize(Roles = "Admin")]` gating on
  `BeersController` is enforced by ASP.NET's middleware pipeline, not the action method, so
  that behavior is covered at the HTTP level via `WebApplicationFactory<Program>` instead of a
  controller unit test. Run locally with `dotnet test beer-app/BeerApi.Tests/BeerApi.Tests.csproj`.
- **Frontend**: Vitest + React Testing Library, tests colocated as `*.test.jsx`/`*.test.js`
  next to the file under test. Page tests mock `src/lib/api.js`; `api.js` itself mocks `fetch`.
  Run locally with `npm test` from `beer-app/frontend` (after `npm install`).
- **CI**: `.github/workflows/tests.yml` runs both suites on every push/PR to `master`.

## Known doc inconsistencies (flagged, not yet fixed)

- Root `README.md` still describes a plain CRUD app and doesn't mention the mug-club
  reframing that the other docs already reflect.
- Frontend port mismatch: `beer-app/README.md` says `:3000`, historical Docker runs exposed
  it at `:3001` — worth double-checking against current `docker-compose.yml` if it matters.
- Root `README.md` is UTF-16 encoded — plain `cat`/grep will show garbled output; use
  `iconv -f UTF-16 -t UTF-8` or an editor that auto-detects encoding.

## Running it locally

```bash
cd beer-app
docker compose up --build
```
Frontend: `http://localhost:3000` (or `:3001` per Docker stack, see port mismatch above) ·
API + Swagger: `http://localhost:5153/swagger` · DB: `localhost:5432`

Manual (no Docker): `dotnet run` in `beer-app/backend/`, and
`npm install && npm run dev` in `beer-app/frontend/`.

## Likely next steps

**Sprint 1 (Mug Club Core) is done** — [PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11)
merged 2026-07-14, issues #2–#6 and the milestone closed. **Sprint 2 (Mug Club Completion)
is groomed** into issues [#12–#16](https://github.com/pmconnolly80/FinalCapstone/milestone/2)
(see `EPICS_AND_SPRINTS.md` and the 2026-07-14 `SESSION_LOG.md` entry). In order:

1. **Implement Sprint 2**, suggested order #12 (PIN lockout — hardens the already-live
   confirm endpoint) → #13 (PIN lifecycle) → #14 (durable mug-earned) → #15 (admin
   audit/correction API) → #16 (admin correction screen, depends on #15). TDD per the
   Definition of Done.
2. Then the named later sprints: Customer Phone Experience (search/availability/OBDB),
   Auth II (social sign-in), Admin Experience, Engagement/Retention/Social.

Local tooling note: only the .NET 10 SDK is on PATH but the projects target net8.0 — run
backend tests with the SDK at `~/.dotnet8` (see `.claude/skills/verify/SKILL.md` for the
exact commands, the curl drive loop, and the dev bartender PIN).
