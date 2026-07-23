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
  - #32 Mobile UX repair — **closes Sprint 3**: `api.js`'s API base URL now derives from
    `window.location.hostname` instead of a literal `localhost` fallback, and
    `docker-compose.yml` no longer overrides it with `VITE_API_URL: http://localhost:5153`
    — a phone opening the app at the host machine's LAN IP now reaches the API there
    instead of at itself (verified live over a LAN IP, not just `localhost`). Auth state is
    now reactive: same-tab login/register/logout dispatch a `beer-auth-changed` window
    event (`api.js`'s new `AUTH_CHANGED_EVENT`/`logout()`) that `App.jsx` listens for,
    replacing the old render-once `getRolesFromToken()` call that went stale until a
    manual reload; nav shows Sign out (not Sign in) once signed in, and Add Beer only for
    Admins. `Home.jsx` is progress-centric for signed-in customers (fetches and renders
    their actual X-of-200 + mug-earned state, reusing `MyProgress.jsx`'s data shape) and
    keeps the existing mug-club pitch for anonymous visitors. Beer CRUD is now actually
    gated off the customer surface: `BeerForm.jsx` renders an "admin account required"
    message instead of the form for non-admins (previously only the nav link was hidden —
    a customer who typed `/beers/new` directly still saw a form that could only ever fail
    server-side). `BeerForm.jsx`/`BeerDetail.jsx`'s remaining `console.error`-only failure
    paths now show a visible message; Name/Brewery/Style are `required`; `AuthPage.jsx`'s
    inputs gained `type="email"`/`autoComplete` hints. New `App.test.jsx` (nav
    auth-awareness, the reactive event).

- **Sprint 4: Auth II** (milestone [#4](https://github.com/pmconnolly80/FinalCapstone/milestone/4),
  closed 2026-07-23, PRs #47–#52):
  - #40/#41 (merged [PR #47](https://github.com/pmconnolly80/FinalCapstone/pull/47)):
    `ApplicationUser : IdentityUser` (`beer-app/backend/Models/ApplicationUser.cs`) with a
    `MarketingConsent` bool (default `false`); replaces the bare `IdentityUser` in
    `ApplicationDbContext`, `Program.cs`'s `AddIdentity<...>`, `AuthController`'s
    `UserManager`/`RoleManager`, and `SeedData.cs`'s dev bartender bootstrap.
    `AddApplicationUserMarketingConsent` migration adds the column (backfills existing rows
    to `false`). `RegisterRequest` gained an optional `MarketingConsent` param — the actual
    consent-checkbox UI is #46's job, this just gives it somewhere to persist. Also
    `IEmailSender`/`SmtpEmailSender` (`beer-app/backend/Services/`), the app's first email
    dependency. Config-driven the same way `CatalogBeerService.cs` handles its API key:
    `Email:SmtpHost`/`SmtpPort`/`Username`/`Password`/`FromAddress`/`FromName`/`EnableSsl` in
    `appsettings.json` (all empty/defaulted in the committed file), overridable via `Email__*`
    in `docker-compose.yml`, sourced from an untracked `beer-app/.env`. Sending silently
    no-ops (logs instead) when `SmtpHost` or `FromAddress` is unconfigured, rather than
    throwing. `ISmtpClient`/`ISmtpClientFactory` wrap `System.Net.Mail.SmtpClient` as a thin
    seam so the send path is unit-testable without a real network call.
  - #42 (merged [PR #48](https://github.com/pmconnolly80/FinalCapstone/pull/48)): `POST /api/auth/forgot-password` — always returns the
    same generic success message regardless of whether the email exists (avoids account
    enumeration), and only sends a reset email via #41's `IEmailSender` when it does.
    `POST /api/auth/reset-password` — validates the Identity reset token via
    `ResetPasswordAsync` (enforces the same length-only min-8 policy as registration since
    it goes through the same password validators); an unknown email returns the identical
    generic "invalid or expired" message an invalid token would, again to avoid enumeration.
    The reset link's frontend origin resolves from `Frontend:BaseUrl` config first (empty by
    default), falling back to the request's `Origin` header (so it works out of the box for
    both `localhost` and a phone hitting the API over a LAN IP, matching #32's dynamic-host
    approach), then `http://localhost:3001` as a last resort. New `ForgotPassword.jsx`/
    `ResetPassword.jsx` pages (`beer-app/frontend/src/pages/`, same inline-styled pattern as
    `AuthPage.jsx`, not yet Tailwind-converted) at `/forgot-password`/`/reset-password`;
    `AuthPage.jsx` login mode links to the former. Backend tests use a new `FakeEmailSender`
    test double (`beer-app/BeerApi.Tests/TestDoubles/`) wired into
    `TestWebApplicationFactory` so integration tests can assert on sent emails without a
    real SMTP server.
  - #43 (merged [PR #49](https://github.com/pmconnolly80/FinalCapstone/pull/49)): Google external sign-in via
    `Microsoft.AspNetCore.Authentication.Google`. New shared
    `IExternalLoginService`/`ExternalLoginService` (`beer-app/backend/Services/`) — the
    link-or-create-by-verified-email rule #44/#45 will reuse: an existing password-auth
    account with the same email gets the external login linked (via Identity's
    `AspNetUserLogins`, no new table) rather than a duplicate account created; a genuinely
    new email creates an `ApplicationUser` with `EmailConfirmed = true` (the provider
    already verified it) in the `Customer` role. `AuthController` gained
    `GET /api/auth/external-login/{provider}` (issues an ASP.NET Core `Challenge`) and
    `GET /api/auth/external-login-callback` (reads the authenticated principal off
    Identity's `IdentityConstants.ExternalScheme` cookie, checks a per-provider
    `IsEmailVerified` gate — Google's userinfo `verified_email` field, mapped to an
    `email_verified` claim in `Program.cs` — then delegates to `ExternalLoginService` and
    redirects to `{frontend}/auth/callback?token=...` with a normal app JWT from the
    existing `CreateToken`). `Authentication:Google:ClientId`/`ClientSecret` follow the
    same empty-by-default/`.env`-sourced secret convention as `CatalogBeer`/`Email`.
    **Caveat:** the challenge endpoint's redirect is verified in tests (a 302 to
    `accounts.google.com`, buildable without real credentials or network access), and
    `ExternalLoginService`'s link/create logic has real integration coverage against the
    app's Identity/EF stack — but the actual callback round-trip against a live Google
    developer-console app hasn't been exercised end-to-end in this environment and needs
    manual verification before real users hit it. No frontend UI yet (buttons, the
    `/auth/callback` receiving page, account linking) — that's #46's job.
  - #44 (merged [PR #50](https://github.com/pmconnolly80/FinalCapstone/pull/50)): Facebook external sign-in via
    `Microsoft.AspNetCore.Authentication.Facebook`, same challenge/callback/
    `ExternalLoginService` pattern as #43 (`Authentication:Facebook:AppId`/`AppSecret`,
    same empty-by-default convention); Facebook's Graph API only ever returns
    addresses it has itself verified, so `IsEmailVerified` just checks the email claim's
    presence for this provider — no separate verified flag to check. Plus, bundled per
    the issue's Facebook app-review requirements: a `PrivacyPolicy.jsx` page at `/privacy`
    (linked from a new app-shell footer), and Facebook's required data-deletion callback —
    `POST /api/auth/facebook/data-deletion` verifies Facebook's signed HMAC-SHA256
    `signed_request` payload (`FacebookSignedRequestParser`, `beer-app/backend/Services/`,
    a pure static parser with no DI so it's directly unit-testable) and, on a match,
    **anonymizes rather than hard-deletes** via new `IAccountDeletionService`/
    `AccountDeletionService`: scrubs email/username to `deleted-{id}@deleted.local`,
    removes the password and every linked external login, but leaves the
    `BeerConfirmation`/`MugAward`/`ConfirmationAudit` rows alone. Those key off
    `CustomerId` (a plain string FK to `AspNetUsers.Id`, not a navigation property with a
    cascade path), and preserving them keeps the tavern's own confirmed-beer ledger
    intact — the same way removing a name from a paper punch-card doesn't erase the
    tavern's own record that a mug was earned. Responds with the
    `{url, confirmation_code}` shape Facebook's contract requires regardless of whether a
    matching account existed, mirroring the same account-enumeration-avoidance pattern as
    `/forgot-password`. **Same caveat as #43**: the challenge/callback wiring against a
    live Facebook app is unverified in this environment; the signed-request verification
    and anonymization logic have real test coverage.
  - #45 (merged [PR #51](https://github.com/pmconnolly80/FinalCapstone/pull/51)): Apple (Sign in with Apple) external sign-in via the
    community `AspNet.Security.OAuth.Apple` package (Apple has no first-party ASP.NET Core
    package), same challenge/callback/`ExternalLoginService` pattern as #43/#44.
    `Authentication:Apple:ClientId`/`TeamId`/`KeyId`/`PrivateKey` follow the same
    empty-by-default convention (`PrivateKey` is the `.p8` key's PEM content with newlines
    escaped as literal `\n` in `docker-compose.yml`/`.env`, since a real multi-line value
    can't round-trip through a single-line env entry — `Program.cs` un-escapes it).
    `GenerateClientSecret = true` has the package build Apple's required JWT client secret
    fresh from that private key on every token exchange — unlike a static secret there's
    nothing to periodically rotate on a schedule; the one real operational follow-up
    (Deployment & Hardening) is updating `Authentication:Apple:PrivateKey` if that key is
    ever revoked/rotated in the Apple Developer portal. Apple's ID token carries its own
    `email_verified` claim, true for both a real address and a Hide My Email privacy relay
    address (Apple guarantees mail sent to a relay reaches the user, so "verified" holds
    either way) — mapped in `AuthController.IsEmailVerified` same as Google/Facebook.
    **Relay-email interaction with the verified-email matching rule (documented, not a
    bug):** a relay address is stable per (app, Apple ID) pair, so repeat Apple sign-ins
    correctly resolve to the same account. But a customer who registered with a password
    using their real email, then later signs in with Apple via a relay address, will
    *not* auto-link — the addresses genuinely differ, so `ExternalLoginService` creates a
    second account rather than linking the first. There's no way to detect this from the
    relay address alone; resolving it needs account-linking UI (#46), not smarter
    matching. **Same caveat as #43/#44**: the challenge/callback wiring against a live
    Apple Developer account is unverified in this environment; `ExternalLoginService`'s
    behavior with the "Apple" provider (including the relay-email non-linking case) has
    real test coverage.
  - #46 (merged [PR #52](https://github.com/pmconnolly80/FinalCapstone/pull/52)) — **closes
    Sprint 4**: wires #43/#44/#45's backend into
    the actual customer-facing auth experience.
    - `AuthPage.jsx` gained three sign-in links (`<a href>`, not a fetch — the whole point
      is a full-page redirect through the provider) pointing at
      `GET /api/auth/external-login/{provider}`, shown in both login and register mode.
      New `AuthCallback.jsx` at `/auth/callback` reads `?token=`/`?error=` (set by
      `ExternalLoginCallback`'s redirect), stores the token and dispatches
      `AUTH_CHANGED_EVENT` the same way password login always has, then bounces to `/`.
      A marketing-consent checkbox (unchecked by default) in register mode now actually
      feeds `RegisterRequest.MarketingConsent` (#40's field, unused until now).
    - **Account linking** — attaching an *additional* provider to an already-signed-in
      account can't reuse the plain sign-in flow (find-or-create by email would risk
      creating a duplicate account instead of linking to the current one), and a
      full-page OAuth redirect can't carry an `Authorization` header. Solved with a
      short-lived, single-use linking ticket in the existing `IMemoryCache` (the same
      cache `CatalogBeerService`/`OpenBreweryDbService` already use): authenticated
      `POST /api/auth/external-login-tickets` mints a ticket bound to the current user;
      the existing `external-login/{provider}` challenge endpoint takes an optional
      `?ticket=` that — if it resolves — carries the user id through the OAuth round trip
      via `AuthenticationProperties.Items` (the same mechanism `RedirectUri` already
      relies on); `ExternalLoginCallback` branches on that to call new
      `ExternalLoginService.LinkAdditionalProviderAsync` (fails cleanly if that
      provider+key is already linked to a *different* account, idempotent if already
      linked to this one) instead of the normal sign-in path, then redirects to
      `/account/linked-providers` rather than issuing a new JWT. Authenticated
      `GET /api/auth/external-logins` lists the signed-in user's linked providers. New
      `LinkedAccounts.jsx` page (route `/account/linked-providers`, linked from a new
      signed-in-only nav item) shows connected/not-connected state per provider and a
      "Connect" button for the rest (`startLinkingProvider` — fetches a ticket, then
      navigates). No JWT is ever placed in a URL by this design (unlike a bearer-token-in-
      query-string alternative that was considered and rejected).
    - **Same caveat as #43/#44/#45**: the live challenge/callback round trip against a
      real Google/Facebook/Apple app is unverified in this environment. The linking
      ticket flow's actual business logic (`LinkAdditionalProviderAsync`'s three
      outcomes, the ticket-creation/listing endpoints' auth gating) has real test
      coverage.

- **Sprint 5: Admin Experience** (milestone [#5](https://github.com/pmconnolly80/FinalCapstone/milestone/5), groomed 2026-07-23, in progress):
  - #53 (`AdminAudit` trail + role assignment API, [PR #60](https://github.com/pmconnolly80/FinalCapstone/pull/60),
    merged): generalized `AdminAudit` entity (`beer-app/backend/Models/AdminAudit.cs`,
    `AddAdminAudit` migration) mirroring Sprint 2's `ConfirmationAudit` shape — actor,
    entity type/id, action, before/after snapshots, required reason, timestamp —
    additive, doesn't replace confirmations' own audit trail. New
    `[Authorize(Roles = "Admin")] AdminUsersController` at
    `PUT /api/admin/users/{id}/role` (`beer-app/backend/Controllers/AdminUsersController.cs`):
    rejects a missing reason or unrecognized role name, replaces the target user's
    existing role(s) via `UserManager`/`RoleManager` (single-role-per-user, matching
    the model used everywhere else in the app), writes the audit row in the same save.
    Foundational for #54/#55 (user management API/screen) and #56/#57 (audited beer
    edit/delete).
  - #54 (API: user management + account actions, [PR #61](https://github.com/pmconnolly80/FinalCapstone/pull/61),
    merged): `GET /api/admin/users` on the same `AdminUsersController` lists every user's
    role, active/locked status, and whether they hold an active staff PIN — reusing
    `StaffPin` rather than duplicating it (a single-query `UserRoles`/`Roles` join for
    role, same join style already used in `StaffPinsController.IssuePin`, to avoid
    per-user round trips). `POST /api/admin/users/{id}/deactivate` /
    `.../reactivate` are reversible: deactivation piggybacks on ASP.NET Identity's own
    lockout mechanism (`LockoutEnd`/`LockoutEnabled` via `UserManager`) rather than a
    bespoke flag — the one place that actually enforces it is `AuthController.Login`,
    which (per this story) now calls `IsLockedOutAsync` before checking the password,
    since `Login` builds its own JWT rather than going through `SignInManager`. Per
    `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 ("deactivating a staff account deactivates
    the PIN everywhere, instantly"), deactivating a bartender/admin also flips their
    `StaffPin.IsActive` to `false` in the same request — mirroring
    `StaffPinsController.DeactivatePin`'s existing logic rather than calling it.
    Reactivating an account clears the lockout but deliberately does **not** restore
    the PIN — silently re-enabling a possibly-compromised PIN isn't safe to do
    implicitly; re-issuing one stays a distinct admin action via the existing PIN
    endpoints. Both actions require a reason and write an `AdminAudit` row, same
    pattern as #53's role assignment.
  - #55 (UI: User Management screen, [PR #62](https://github.com/pmconnolly80/FinalCapstone/pull/62),
    merged): new `beer-app/frontend/src/pages/AdminUsers.jsx` at `/admin/users` (nav entry
    next to the existing "Admin" link, admin-gated the same convenience-check way
    `AdminConfirmations.jsx` is — the API enforces Admin server-side regardless). Follows
    `AdminConfirmations.jsx`'s exact shape: gate → load → table → per-row two-step
    reason-guarded actions. One shared `pendingAction` state (`{ userId, type, role?,
    pin? }`) covers all four gated actions per row (role change via a `<select>`,
    deactivate, reactivate, PIN issue/reset) since there are several here instead of
    just one; PIN deactivate needs no reason (the API doesn't require one) so it's a
    direct button with no guard step. "Set PIN"/"Deactivate PIN" only render for
    Bartender/Admin rows, matching `StaffPinsController.IssuePin`'s own staff-only
    restriction; "Reactivate" only for inactive rows, "Deactivate" only for active
    ones. 6 new `src/lib/api.js` functions (`getAdminUsers`, `assignRole`,
    `deactivateAccount`, `reactivateAccount`, `issueOrResetStaffPin`,
    `deactivateStaffPin`), each mirroring `voidConfirmation`'s error-surfacing shape.
    **Bug found and fixed during this story's manual verification** (not a regression
    from #54, just never exercised until a real multi-role user existed): `GetUsers`
    used `ToDictionaryAsync` to build the per-user role lookup, which throws if any
    user ever has more than one role row (the app's own `AssignRole` never creates
    this, but a manual DB correction could) — 500ing the *entire* list for every user,
    not just the affected one. Switched to a `GroupBy`-then-`ToDictionary` that just
    picks one role, with a regression test.
  - #56 (API: audited beer edit/delete + inline availability update,
    [PR #63](https://github.com/pmconnolly80/FinalCapstone/pull/63), merged): `BeersController`'s
    existing `PUT`/`DELETE` (already `[Authorize(Roles = "Admin")]`, previously with zero
    audit trail) now write an `AdminAudit` row each. Edits log a **changed-fields-only
    text diff** (e.g. `"Style: Amber Ale; Abv: 5.2"` → `"Style: Belgian Pale Ale; Abv:
    5.5"`, via a new private `DescribeBeerDiff` helper) rather than this codebase's first
    JSON-blob snapshot — #53/#54's `BeforeSnapshot`/`AfterSnapshot` only ever held short
    plain strings, so a whole-entity diff follows that same tone instead of introducing a
    new format; a no-op edit (identical resubmission) writes no audit row at all. `PUT`
    also now 404s on an unknown id (previously threw `DbUpdateConcurrencyException` — the
    existing-row fetch needed for diffing makes this check free). `DELETE` gains a
    required `reason` query parameter (no existing frontend call to break — matches
    `GetBeers`'s plain-query-param convention rather than inventing a DELETE-with-body
    shape). New `PATCH /api/beers/{id}/availability` (`UpdateAvailabilityRequest`) is the
    single-field inline-toggle endpoint #57's table needs; a same-value PATCH is a no-op,
    also audited, no reason required (only delete requires one, per the issue).
  - #57 (UI: Beer Management Table, [PR #64](https://github.com/pmconnolly80/FinalCapstone/pull/64),
    merged): new `beer-app/frontend/src/pages/AdminBeers.jsx` at `/admin/beers`
    ("Manage Beers" nav entry), same admin-gate-then-load shape as
    `AdminUsers.jsx`/`AdminConfirmations.jsx`. Reuses `searchBeers` (defaulting
    `availability: 'all'`, not the customer default of in-stock-only) for search;
    reuses the existing `BeerForm.jsx` unchanged for Add/Edit (just its post-save
    redirect target moved from `/beers` to `/admin/beers`). Availability changes fire
    immediately on `<select>` change via #56's `PATCH .../availability` — deliberately
    **not** run through a reason guard, since that endpoint doesn't require one; Delete
    is the one action that does, reusing `AdminUsers.jsx`'s exact two-step
    `pendingAction` guard pattern. This closes out the "customer-surface remnants of
    beer CRUD" per `MVP_SCREEN_PLAN.md` — turned out to be narrower than it sounds:
    `BeerList.jsx` itself had no CRUD markup to remove; the only actual remnant was the
    admin-gated "Add Beer" link sitting in the main nav bar (removed) rather than under
    the admin section.
  - #58 (API: anomaly detection, [PR #65](https://github.com/pmconnolly80/FinalCapstone/pull/65),
    merged): new `GET /api/admin/anomalies` (`AdminAnomaliesController`), computed
    on-demand from existing tables — no new tables, no background job (nothing in this
    issue's scope needs persisted state; §4.5's fuller "background job pipeline + push
    delivery" vision is a later epic). Three signals, each a `public static` method
    taking an explicit `DateTime now` parameter rather than reading `DateTime.UtcNow`
    internally, so unit tests are deterministic regardless of when they run: bulk
    beer-add bursts (bucketed by `Anomalies:BulkBeerAdd:WindowMinutes`, attributed to an
    admin via the new `PostBeer` audit row below), confirmation velocity spikes
    (overall and per-bartender, each against its own trailing baseline average, with a
    `MinimumCount` floor so a near-zero baseline doesn't trip on 1-2 confirmations), and
    off-hours confirmations (config-driven tavern hours that can wrap past midnight,
    e.g. open 10am/close 2am, with an optional `TimeZoneId` since `ConfirmedAt` is
    stored in UTC but "off-hours" is inherently local time). Thresholds live under a
    new `Anomalies` section in `appsettings.json`, same `IConfiguration`-direct-read
    pattern as `CatalogBeer`/`Email` (no options-class binding exists anywhere in this
    codebase). **Also extends `PostBeer` to write an `AdminAudit` row** (`Action =
    "Create"`) — a gap found during this story's investigation: neither `Beer` nor any
    audit table recorded who created a beer before this (#56 only audited `PUT`/
    `DELETE`), which would have made bulk-add anomalies impossible to attribute to an
    admin account. Verified live: a real burst of 10 beers correctly fired a
    `BulkBeerAdd` anomaly attributed to the actual admin account.
  - #59 (UI: Admin Dashboard, [PR #66](https://github.com/pmconnolly80/FinalCapstone/pull/66),
    open) — **closes Sprint 5**: new `beer-app/frontend/src/pages/AdminDashboard.jsx`
    at `/admin/dashboard`, backed by a new `GET /api/admin/dashboard/summary`
    (`AdminDashboardController`) rather than stitching together client-side counts from
    other screens' list endpoints — none of which have a cheap count-only path, and
    "active members" wasn't computable from any existing endpoint at all. Real
    `COUNT`/`COUNT(DISTINCT ...)` queries: total beers (all, regardless of
    availability), confirmations today (UTC calendar day), active members (distinct
    customers with ≥1 confirmation in the last 30 days — the codebase's own
    `PERSONAS_AND_USAGE.md` defines "active member" as engagement-based, contrasted
    with "lapsed," not an account-status flag, so this implements that real definition
    rather than the cheaper "non-deactivated account" reading), and mugs awarded. Same
    `public static` + explicit `DateTime now` testability pattern as #58's
    `AdminAnomaliesController`. The anomaly panel calls #58's
    `GET /api/admin/anomalies` directly and renders each item's `DeepLink` as a
    `<Link>` — no transformation needed. The summary fetch and the anomalies fetch are
    independent `.then/.catch` chains (not a single `Promise.all`), so one endpoint
    failing only blanks its own section. **`Home.jsx` now redirects Admin-role users to
    `/admin/dashboard` on load** (it previously showed every signed-in user, admins
    included, the customer progress card — a real gap, since "becomes the landing page
    for the Admin role" wasn't true until this change) — new `getRolesFromToken()`
    check + `useNavigate`, confirmed with the user before implementing since it's a
    genuine behavior change, not just a nav-link addition. Verified live: dashboard
    numbers matched a direct `psql` cross-check exactly, and the anomaly panel rendered
    the live `BulkBeerAdd` anomaly from #58's own smoke test with a working link.

**Not built** — the Admin Experience epic is done as of Sprint 5. Next up per
`EPICS_AND_SPRINTS.md`: Engagement, Retention & Social (not yet groomed into issues).

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

## Planning conventions

Plan files (`.claude/plans/*.md`) and `SESSION_LOG.md` entries must never present
pseudocode or a design sketch as if it were finished, working code. If a draft plan
needs a rough sketch to work through an approach, label it explicitly as a sketch (e.g.
"pseudocode — not real code" / "sketch, see below for the actual version") rather than
putting it in the same code-fence style as intended-to-be-correct code. This surfaced
during #58's planning: an earlier draft left a broken, unlabeled `.Cast<...>().Append(...)`
sketch in a controller-code block, indistinguishable from the real logic around it — a
follow-up session (or the same one, after context compaction) could easily have
mistaken it for decided, working code and implemented it as-is.

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

**Sprints 1 through 5 are all done.** Sprint 1
([PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11), 2026-07-14), Sprint 2
(PRs #19–#25, milestone closed 2026-07-15), Sprint 3: Customer Phone Experience
(PRs #33–#39, issues #26–#32, closed 2026-07-21; suites at close: backend 131/131,
frontend 99/99), **Sprint 4: Auth II** (milestone
[#4](https://github.com/pmconnolly80/FinalCapstone/milestone/4), issues #40–#46, groomed
2026-07-21, closed 2026-07-23 — PRs #47–#52; suites at close: backend 171/171,
frontend 117/117), and **Sprint 5: Admin Experience** (milestone
[#5](https://github.com/pmconnolly80/FinalCapstone/milestone/5), issues #53–#59, groomed
2026-07-23, closed 2026-07-23 — PRs #60–#66; suites at close: backend 236/236,
frontend 149/149). See `EPICS_AND_SPRINTS.md` and `SESSION_LOG.md` for the full history.

Sprint 5 built: a generalized `AdminAudit` trail + role assignment (#53) → user
management/account actions API (#54) and screen (#55); audited beer edit/delete +
inline availability (#56) → Beer Management Table (#57), which relocated the last
customer-surface beer-CRUD remnant off the nav bar; anomaly detection (#58,
informational — bulk beer-add, confirmation velocity spikes, off-hours activity) →
Admin Dashboard (#59), which also made itself the actual landing page for the Admin
role (`Home.jsx` now redirects admins there) and closed the sprint. See the Sprint 5
bullets above for what each story built.

Next up: the **Engagement, Retention & Social** epic (milestone badges, push
notifications + owner composer, My Beers, social feed, journal, owner analytics) is the
next candidate for grooming into a sprint — per this repo's convention
(`EPICS_AND_SPRINTS.md`), only the next epic gets fully broken into GitHub issues once
it's actually up, and that grooming session hasn't happened yet. Deployment & Hardening
follows after that.

Local tooling note: only the .NET 10 SDK is on PATH but the projects target net8.0 — run
backend tests with the SDK at `~/.dotnet8` (see `.claude/skills/verify/SKILL.md` for the
exact commands, the curl drive loop, and the dev bartender PIN).
