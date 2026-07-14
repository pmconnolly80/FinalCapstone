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
- **Planning docs** (root level, flat, no `docs/` folder): `PROJECT_PLAN.md`,
  `TECHNICAL_ARCHITECTURE_PLAN.md`, `FEATURE_MAP.md`, `IMPLEMENTATION_BACKLOG.md`,
  `MVP_SCREEN_PLAN.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`, `PRODUCT_FLOW_DIAGRAM.md`,
  `PHASE1_IMPLEMENTATION_CHECKLIST.md`, `PROGRESS_TRACKER.md`.

Suggested reading order for onboarding: `PROJECT_PLAN.md` → `PROGRESS_TRACKER.md` →
`FEATURE_MAP.md` / `IMPLEMENTATION_BACKLOG.md` for backlog detail → `beer-app/README.md`
for run instructions.

## Current implementation status (verified against code, not docs)

**Built:**
- `Beer` model — `Id, Name, Brewery, Style, Description, CreatedAt`
  (`beer-app/backend/Models/Beer.cs`)
- `BeersController` — GET all / GET by id (anonymous), POST/PUT/DELETE (`[Authorize]`)
  (`beer-app/backend/Controllers/BeersController.cs`)
- `AuthController` — `/api/auth/register` and `/api/auth/login`, JWTs via `IdentityUser`
  (`beer-app/backend/Controllers/AuthController.cs`)
- React pages: beer list, beer detail, create/edit form, login/register
  (`beer-app/frontend/src/pages/`)
- Docker Compose wiring for db/api/web

**Not built** — documented across the planning docs as the primary MVP driver, but a grep
for "confirm|mug|tavern" across the entire backend and frontend source returns zero hits:
- No `BeerConfirmation` or `Tavern`/`Location` entity
- No bartender confirm-a-beer-for-a-customer flow
- No customer "X of 200" progress view or "mug earned" milestone
- No user roles at all — no `[Authorize(Roles=...)]` anywhere in `beer-app`
- No Open Brewery DB API integration (flagged in docs as a future scope item, revisit in
  next planning pass)

**Known gap:** `beer-app/backend/Data/` has no `Migrations/` folder, but `Program.cs` calls
`db.Database.Migrate()` on startup. A fresh database will not get a schema until someone
runs `dotnet ef migrations add Initial`.

## Known doc inconsistencies (flagged, not yet fixed)

- Root `README.md` and `PROGRESS_TRACKER.md` still describe a plain CRUD app and don't
  mention the mug-club reframing that the other docs and the latest commit already reflect.
- `PHASE1_IMPLEMENTATION_CHECKLIST.md` has every box unchecked even though
  `PROGRESS_TRACKER.md` confirms most of that work (scaffolds, CRUD API, auth endpoints,
  Docker) is done.
- Frontend port mismatch: `beer-app/README.md` says `:3000`, `PROGRESS_TRACKER.md` says the
  Docker stack exposed it at `:3001`.
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

Per `PROGRESS_TRACKER.md`'s own "suggested next focus," and given mug-club/bartender
confirmation is the stated primary driver with zero code yet, the realistic paths forward are:
- Start Epic 2.5 from `PROJECT_PLAN.md` (mug club progress + bartender confirmation) — the
  actual product differentiator, currently 0% built.
- Or harden the existing CRUD/auth foundation first (seed data, add EF Core migrations,
  add role-based auth) before layering on the mug-club feature.
