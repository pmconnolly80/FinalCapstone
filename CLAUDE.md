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
  - `frontend/` — React 18 + Vite 5 + react-router-dom 6, **Tailwind CSS v4**
    (`@tailwindcss/vite`, adopted 2026-07-14 in PR #19; Home + app shell use it, the older
    pages are still inline-styled until the Customer Phone Experience sprint).
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
- **Sprint 2 interrupts** (2026-07-14 live testing, both merged same day — see the bug
  convention now in `EPICS_AND_SPRINTS.md`):
  - [PR #19](https://github.com/pmconnolly80/FinalCapstone/pull/19) (#18): **Tailwind CSS v4**
    adopted (`@tailwindcss/vite`); `/` is a real `Home.jsx`; app shell restyled; `index.css`
    has a compatibility base layer restoring browser defaults preflight strips (old pages are
    still inline-styled — full restyle is Customer Phone Experience scope)
  - [PR #20](https://github.com/pmconnolly80/FinalCapstone/pull/20) (#17): `register()`/`login()`
    in `api.js` surface the API's `message`; **password policy is explicit length-only min 8**
    (`Program.cs`, kept in sync with the AuthPage hint + client-side check)
- **Sprint 2: Mug Club Completion** (closed 2026-07-15, PRs #21–#25 — mug-club epic done):
  - #12 PIN lockout, two axes: per-PIN (`StaffPin.FailedAttempts`/`LockedUntil`, 5 fails →
    15 min) + per-customer rolling window (`FailedConfirmationAttempt` table); all
    rejections are the same generic 401, real reasons recorded server-side
  - #13 PIN lifecycle: `StaffPinsController` — staff `PUT /api/staff-pins/me`, admin
    `PUT`/`DELETE /api/staff-pins/{userId}`; unique among active PINs; "My PIN" screen at
    `/my-pin`
  - #14 durable mug-earned: `MugAward` stamped exactly once at the 200th confirmation;
    progress reads the award, never the count; `GET /api/mug-awards` (Admin) earner list
  - #15/#16 admin correction: `AdminConfirmationsController` (filterable list, audits,
    `POST {id}/void` with **required reason**; void frees the beer for re-confirmation,
    award never revoked — see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1) + the
    `/admin/confirmations` screen (two-step void guard, role-aware nav via
    `getRolesFromToken()`)
- **Sprint 3: Customer Phone Experience** (groomed 2026-07-20, milestone
  [#3](https://github.com/pmconnolly80/FinalCapstone/milestone/3), in progress):
  - #26 `Beer.Availability` (`OnTap`/`Available`/`OutOfStock`/`Retired`, `AddBeerAvailability`
    migration, defaults to `Available`, stored as text via `HasConversion<string>`); the enum
    carries `[JsonConverter(typeof(JsonStringEnumConverter))]` so the API always serializes it
    as a string regardless of caller JSON options — no admin/search UI on top of it yet, that's
    #27–#28
  - #27 `GET /api/beers` is now the search endpoint: `search` (name/brewery/style substring,
    case-insensitive), `availability` (specific state, or `all` to bypass the default;
    omitted defaults to in-stock — `OnTap`/`Available` only), `hadStatus` (`had`/`nothad`,
    requires an authenticated customer or 401), `page`/`pageSize` (default 200, matching the
    tavern's ~200-beer catalog so today's UI stays unpaginated in practice). Response is a
    `BeerSearchResponse` envelope (`items`/`page`/`pageSize`/`totalCount`); each item carries
    a `confirmed` flag for the calling customer, false if anonymous
    (`beer-app/backend/Controllers/BeersController.cs`)
  - #28 `BeerList.jsx` rebuilt around the search endpoint (Tailwind, replacing the last
    inline-styled beer-facing page): debounced search-as-you-type, an availability chip
    row (`In Stock`/`On Tap`/`Available`/`Out of Stock`/`Retired`/`All`), a had/not-had
    chip row (signed-in customers only, since `hadStatus` 401s anonymously), and
    style/brewery "quick-search" chips computed from the current result page (the API has
    one combined `search` field, not separate style/brewery params, so these just fill the
    search box rather than compose as independent structured filters). Each result shows
    an availability badge and a confirmed checkmark. `fetchBeers()` → `searchBeers(params)`
    in `api.js`, returning the full envelope
  - #29 Beer-nerd stats + Open Brewery DB brewery card: `Beer` grows `Abv` (double?),
    `Ibu` (int?), `StyleFamily` (string?), `Class` (nullable `BeerClass` enum — `Ale`/
    `Lager`, stored as text like `Availability`), `ObdbBreweryId` (string?)
    (`AddBeerNerdStatsAndObdbBreweryId` migration). New `IBreweryLookupService` /
    `OpenBreweryDbService` (`beer-app/backend/Services/`) proxies and caches (`IMemoryCache`,
    24h TTL) `GET api.openbrewerydb.org/v1/breweries/{id}` — any failure (404, network down,
    bad JSON) degrades to a `null` brewery card rather than breaking beer detail; the first
    `AddHttpClient`/external-API integration in the backend. `GET /api/beers/{id}` now
    returns `BeerDetailResponse` (nerd-stat fields + a resolved `BreweryInfo?`) instead of
    the raw `Beer` entity — `PostBeer`/`PutBeer` stay on the raw entity, unaffected.
    `BeerDetail.jsx` renders a nerd-stats block and brewery card (with website link) when
    present; `BeerForm.jsx` gained ABV/IBU/style-family/class inputs (OBDB brewery id has
    no form control yet — that's #30's autocomplete)
  - #30 Admin Open Brewery DB brewery autocomplete: `IBreweryLookupService` gained
    `SearchBreweriesAsync(query)` (`GET breweries/search?query=`, same cache, keyed by the
    normalized query — failures return an empty list, never throw); new
    `[Authorize(Roles = "Admin")] BreweriesController` at `GET /api/breweries/search`
    (`beer-app/backend/Controllers/BreweriesController.cs`). `BeerForm.jsx`'s Brewery field
    is now a debounced (300ms) autocomplete: typing shows a suggestion dropdown (name +
    city/state), selecting one fills the field and stores `ObdbBreweryId`; typing further
    by hand clears the stored id (manual entry always stays the override, never blocked by
    a stale link) — `searchBreweries(query)` added to `api.js`
  - #31 Catalog.beer beer-level pre-fill (**spike result: GO** — 6/8 clear hits, 1/8 close,
    1/8 miss on the seeded list; see `TECHNICAL_ARCHITECTURE_PLAN.md` §6 for the full
    finding): new `ICatalogBeerService`/`CatalogBeerService` (Basic auth, API key as
    username, same `IMemoryCache` pattern as OBDB, cb_verified-first sort) at
    `beer-app/backend/Services/CatalogBeerService.cs`; `[Authorize(Roles = "Admin")]
    CatalogBeerController` at `GET /api/catalog-beer/search`. **The API key is a real
    secret — never committed.** It's read from `CatalogBeer:ApiKey` config (empty string
    in the committed `appsettings.json`), overridable via `CatalogBeer__ApiKey`/
    `CATALOG_BEER_API_KEY` in `docker-compose.yml`, sourced from an untracked `beer-app/.env`
    (`.env`/`.env.*` added to `.gitignore`) — without a key configured, the service just
    returns an empty list, so the feature silently no-ops rather than breaking. `BeerForm.jsx`'s
    Name field triggers a debounced Catalog.beer search; selecting a result pre-fills style/
    ABV/IBU/style-family/class/description (the admin verifies and can always override) with
    a CC BY 4.0 attribution line — `searchCatalogBeer(query)` added to `api.js`

**Not built** — next up per `EPICS_AND_SPRINTS.md`:
- No admin UI to assign roles (currently DB-manual only; PIN management API exists)

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

- None currently. (The three long-standing ones — root `README.md` describing the old CRUD
  app, its UTF-16 encoding, and the `:3000`/`:3001` port mismatch in `beer-app/README.md` —
  were all fixed 2026-07-15: the root README is now UTF-8 and mug-club-framed, and both
  READMEs state the real port mapping: `:3001` via Docker, `:3000` via `npm run dev`.)

## Running it locally

```bash
cd beer-app
docker compose up --build
```
Frontend: `http://localhost:3001` via Docker (`npm run dev` outside Docker serves `:3000`) ·
API + Swagger: `http://localhost:5153/swagger` · DB: `localhost:5432`

Manual (no Docker): `dotnet run` in `beer-app/backend/`, and
`npm install && npm run dev` in `beer-app/frontend/`.

## Likely next steps

**Sprints 1 and 2 are both done and the Mug Club epic is complete** — Sprint 1
([PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11), 2026-07-14) and Sprint 2
(PRs #19–#25, milestone closed 2026-07-15; suites at close: backend 85/85, frontend 61/61).
**Sprint 3: Customer Phone Experience** is groomed and underway (milestone
[#3](https://github.com/pmconnolly80/FinalCapstone/milestone/3), issues #26–#32, groomed
2026-07-20). See `EPICS_AND_SPRINTS.md` and `SESSION_LOG.md`. In order:

1. #26–#31 done (`Beer.Availability` data model, beer search API, search-first list UI,
   beer-nerd stats + OBDB brewery card, admin OBDB brewery autocomplete, Catalog.beer
   pre-fill spike — GO) — backend 131/131, frontend 84/84. Next: #32 (mobile UX repair
   bundle) — the last story in Sprint 3.
3. Then the remaining named sprints: Auth II (social sign-in + password reset), Admin
   Experience, Engagement/Retention/Social, Deployment & Hardening.

Local tooling note: only the .NET 10 SDK is on PATH but the projects target net8.0 — run
backend tests with the SDK at `~/.dotnet8` (see `.claude/skills/verify/SKILL.md` for the
exact commands, the curl drive loop, and the dev bartender PIN).
