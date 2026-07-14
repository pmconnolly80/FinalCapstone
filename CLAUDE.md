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
  - `frontend/` — React 18 + Vite 5 + react-router-dom 6, inline styles (no UI framework).
  - `docker-compose.yml` — `db` (postgres:16-alpine), `api`, `web` services.
  - `infra/aws-architecture.md` — deployment design doc only, no actual IaC yet.
- **`BeerList/`** — legacy pre-refactor ASP.NET MVC 5 / EF6 app. Kept as historical
  reference only; not where new work happens. Notably it had `[Authorize(Roles = "canEdit")]`
  role gating that the new `beer-app` backend does not currently have.
- **Planning/vision docs** (root level, flat, no `docs/` folder): `PROJECT_PLAN.md`,
  `TECHNICAL_ARCHITECTURE_PLAN.md`, `FEATURE_MAP.md`, `IMPLEMENTATION_BACKLOG.md`,
  `MVP_SCREEN_PLAN.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`, `PRODUCT_FLOW_DIAGRAM.md`.
  These describe the target product and haven't gone stale the way a status tracker does.
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
- **Note:** the auth/roles/migrations/seed work above is complete on the `harden-foundation`
  branch (PR open) but not yet merged to `master` — check which branch you're on.

**Not built** — the primary MVP driver per the planning docs, and the active work: Sprint 1 in
`EPICS_AND_SPRINTS.md` (GitHub issues [#2](https://github.com/pmconnolly80/FinalCapstone/issues/2)–[#6](https://github.com/pmconnolly80/FinalCapstone/issues/6)):
- No `BeerConfirmation` or `Tavern`/`Location` entity
- No bartender confirm-a-beer-for-a-customer flow
- No customer "X of 200" progress view or "mug earned" milestone
- No admin UI to assign roles (currently DB-manual only) or audit/correct confirmations
- No Open Brewery DB API integration (flagged in docs as a future scope item, revisit in
  next planning pass)

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

The foundation (migrations, seed data, role-based auth) is hardened — see `harden-foundation`
branch above. Current active work is **Sprint 1: Mug Club Core** (the actual product
differentiator, per `EPICS_AND_SPRINTS.md`): merge `harden-foundation` into `master` first
(the mug-club API depends on the role-gating it adds), then work GitHub issues
[#2](https://github.com/pmconnolly80/FinalCapstone/issues/2)–[#6](https://github.com/pmconnolly80/FinalCapstone/issues/6)
in order — data model, bartender confirm endpoint, customer progress endpoint, then the two
matching UI screens.
