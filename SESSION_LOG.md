# Session Log

A dated entry per working session: what sprint/epic it touched, what changed, and links to the
commits/PRs/issues involved. Kept in the repo itself (not just GitHub) so the alignment between
"what got worked on" and "what the plan says is next" is readable in one place, in order.

---

## 2026-07-13 — Repo audit, add root `CLAUDE.md`

**Epic:** none yet formalized (pre-dates `EPICS_AND_SPRINTS.md`)

Audited the whole repo to get oriented: which codebase is active (`beer-app/` vs. legacy
`BeerList/`), what's actually built in code vs. only described in the planning docs, and where
the docs themselves had drifted (README/`PROGRESS_TRACKER.md` not reflecting the mug-club
reframing, `PHASE1_IMPLEMENTATION_CHECKLIST.md` fully unchecked despite completed work, a
frontend port mismatch). Wrote `CLAUDE.md` at the repo root to capture all of this for future
sessions.

- Commit: `cce4c74` — *docs: add root CLAUDE.md documenting project state and doc/code gap*

## 2026-07-13 — Harden the foundation: migrations, seed data, role-based auth

**Epic:** Auth & Roles (`epic:auth`)

Closed three gaps found during the audit: no EF Core migrations existed at all (a fresh
database got zero tables), no seed data, and Identity roles were wired up but never created,
assigned, or checked anywhere. Added the initial migration, seeded the `Admin`/`Bartender`/
`Customer` roles plus sample beers, assigned new registrations to `Customer`, put role claims
in the JWT, and gated the catalog's write endpoints to `Admin`. Also fixed the frontend never
attaching its JWT on beer create/edit requests, found while testing the new role gate
end to end. Verified live against Docker: a plain customer got `403` creating a beer, an
Admin-promoted account got `201`.

- Branch: `harden-foundation` (pushed, PR open, not yet merged to `master`)
- Commits: `cce4c74` (CLAUDE.md, carried onto this branch), `831e78a` — *feat: harden beer-app
  foundation with migrations, seed data, role-based auth*

## 2026-07-13 — Establish Agile process: epics, sprints, GitHub tracking

**Epic:** process/tracking itself, not a product epic

Set up formal Agile tracking so future sessions have a visible plan to stay aligned to, instead
of the ad hoc planning docs alone. Mapped epics to GitHub labels and sprints to GitHub
milestones (scope-based, not calendar-based, since this is solo session-based work), wrote
`EPICS_AND_SPRINTS.md` as the markdown mirror of that board, and started this log. Retired
`PROGRESS_TRACKER.md` and `PHASE1_IMPLEMENTATION_CHECKLIST.md` (both flagged stale in
`CLAUDE.md`) in favor of this doc and the GitHub milestone/issues as the single source of
truth for status.

- Created labels: `epic:core-catalog`, `epic:auth`, `epic:mug-club`, `epic:discovery`,
  `epic:admin`, `epic:deployment`, `epic:future-enhancements`
- Created milestone: [Sprint 1: Mug Club Core](https://github.com/pmconnolly80/FinalCapstone/milestone/1)
- Created issues: [#2](https://github.com/pmconnolly80/FinalCapstone/issues/2),
  [#3](https://github.com/pmconnolly80/FinalCapstone/issues/3),
  [#4](https://github.com/pmconnolly80/FinalCapstone/issues/4),
  [#5](https://github.com/pmconnolly80/FinalCapstone/issues/5),
  [#6](https://github.com/pmconnolly80/FinalCapstone/issues/6)

## 2026-07-13 — Merge `harden-foundation` into `master`

**Epic:** Auth & Roles (`epic:auth`)

Opened and merged [PR #7](https://github.com/pmconnolly80/FinalCapstone/pull/7), bringing the
migrations/seed-data/role-based-auth work onto `master`. Sprint 1's API stories (bartender
confirm endpoint, customer progress endpoint) depend on the role gating this adds, so this had
to land before that work starts. Also committed the Agile process docs
(`EPICS_AND_SPRINTS.md`, `SESSION_LOG.md`, retired-doc stubs, updated `CLAUDE.md`) directly to
`master`, since they document process rather than product code.

- Merge commit: `526b7b9`
- Docs commit: `50ca9c1` — *docs: establish Agile process with epics, sprints, and session
  logging*

## 2026-07-13 — Verify `beer-app` runs end-to-end via Docker Compose

**Epic:** none (environment verification, not product work)

Ran the full stack from `master` to confirm the merged foundation work (migrations, seed data,
role-based auth) actually stands up cleanly, ahead of starting Sprint 1's confirm-endpoint work.

- `cd beer-app && docker compose up --build -d` — built `api` and `web` images, started `db`
  (`postgres:16-alpine`), `api`, `web`; all three came up healthy.
- Smoke-tested: `GET /api/beers` → `200` with seeded beers (Dogfish Head 60 Minute IPA, Fat
  Tire, Duvel, etc.), `/swagger/index.html` → `200`, frontend root → `200`.
- Confirmed the port mismatch already flagged in `CLAUDE.md`: compose maps the frontend to
  host port **3001**, not the `3000` the README states.
- Shut down cleanly afterward (`docker compose down`, no `-v`, so the `pgdata` volume and its
  seeded rows persist) so next session starts from a known-clean state.

**To resume:** `cd beer-app && docker compose up --build` — frontend at `localhost:3001`, API/
Swagger at `localhost:5153/swagger`, db at `localhost:5432`. No code changes this session, so no
new commit.

## 2026-07-13 — Product re-plan: phone-first UX, Open Brewery DB scoping, retention features + full code audit

**Epic:** planning (touches `epic:phone-experience` and `epic:retention`, both newly named)

Planning session prompted by dissatisfaction with the current app's real-world usability: it
reads as an admin CRUD catalog, not an app on a customer's phone at the bar. Re-planned the
product around the customer's defining moment — order a beer, **search** for it, read about
it, get it bartender-confirmed — and added an engagement/retention feature set that makes the
app pay off for the bar owner (badges, notifications, challenges, QR membership card, owner
analytics). Docs updated: `FEATURE_MAP.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`,
`MVP_SCREEN_PLAN.md`, `IMPLEMENTATION_BACKLOG.md`, `PROJECT_PLAN.md`, `EPICS_AND_SPRINTS.md`
(two new named epics, not yet ticketed per the grooming rule).

**Open Brewery DB scoped (was "revisit next planning pass"):** verified against the live API —
`api.openbrewerydb.org/v1` serves **breweries only** (name, type, address, geo, website);
there is no beer-level endpoint (`/v1/beers` → 404). Decision: the tavern's list stays the
source of truth for the ~200 beers; OBDB enriches beer detail pages with brewery info and
powers brewery autocomplete in the admin add/edit form, with server-side caching.

**Code audit (subagent, findings verified against source):** highest-priority items —
1. JWT signing key committed and used as the live fallback (`appsettings.json`, `Program.cs`,
   `AuthController.cs`); compose never overrides it.
2. No path to an Admin/Bartender user exists (register hardcodes `Customer`, seed assigns no
   users to roles) → every `[Authorize(Roles = "Admin")]` write endpoint is unreachable.
3. Frontend API base URL is `http://localhost:5153` baked into the bundle → the app cannot
   work from a real phone.
4. Bug: `saveBeer` in `frontend/src/lib/api.js` parses JSON on the PUT's `204 No Content`,
   so edits appear to fail even when they succeed.
5. No search/filter on the beer list; nav is auth-blind (always shows "Sign in"/"Add Beer");
   errors go to `console.error` only; CORS is `AllowAnyOrigin`.
Fix homes: mobile blockers → Customer Phone Experience sprint; security items → Deployment &
Hardening sprint (both named in `EPICS_AND_SPRINTS.md`).

No product code changed this session — docs only.

## 2026-07-13 — Feature expansion: one-device PIN confirmation, push, social layer, rotating inventory, social sign-in + persona deep dive

**Epic:** planning (reshapes `epic:mug-club` Sprints 1–2; expands `epic:auth`,
`epic:phone-experience`, `epic:admin`, `epic:retention`)

Planning session adding five feature ideas to the product, expanded into concrete usage via
a new persona deep-dive doc (`PERSONAS_AND_USAGE.md` — customer/bartender/owner/admin
day-in-the-life narratives):

1. **One-device confirmation rule (the big one, decided mid-session):** everything happens
   on the customer's phone — no bar tablet, no bartender screens. The bartender types their
   personal **6-digit PIN** on the customer's phone to confirm a beer (customer from JWT
   session, bartender resolved from PIN). Supersedes the earlier "I'm drinking this"
   request-queue and QR-membership-card designs. Threat model + two-axis lockout +
   velocity/anomaly flags in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1; alternatives kept in
   `PERSONAS_AND_USAGE.md` §6. **GitHub issues #3/#6 need re-scoping to match** (flagged in
   `EPICS_AND_SPRINTS.md`, issues not yet edited).
2. **Push notifications:** frontend becomes an installable PWA; Web Push w/ VAPID,
   `PushSubscription` entity, owner-composed sends with consent-gated audience targeting
   plus automated nudges, frequency caps (§4.2).
3. **Social layer (opt-in, default private):** display name, system-generated milestone
   feed (no free-text posts), cheers, leaderboard, communal goal widget, wall of mugs (§4.3).
4. **Rotating inventory:** `Beer.Availability` (on tap/available/out of stock/retired),
   in-stock-by-default search, confirmations permanent through churn (§4.4); plus a
   **catalog guardrail** — an abnormal burst of beer-adds notifies owner+admin (§4.5), and
   **admin can edit all data** to fix inaccuracies/questionable submissions, audited with
   reason notes.
5. **Social sign-in (researched):** Google/Facebook/Apple via ASP.NET Core Identity
   external login providers (recommended over Auth0/Clerk/Firebase/Cognito for a single
   tavern — Identity+JWT already wired, no vendor), account linking on verified email,
   marketing-consent checkbox at signup (§4.6).

Docs updated: `PERSONAS_AND_USAGE.md` (new), `TECHNICAL_ARCHITECTURE_PLAN.md` (§4.1–4.6),
`FEATURE_MAP.md`, `PROJECT_PLAN.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`,
`MVP_SCREEN_PLAN.md`, `IMPLEMENTATION_BACKLOG.md`, `PRODUCT_FLOW_DIAGRAM.md` (flows redrawn
for the single-phone model), `EPICS_AND_SPRINTS.md` (epic table + Sprint 2 scope + later
sprints; new "Auth II: Social Sign-in" sprint named).

No product code changed this session — docs only.

## 2026-07-14 — Beer-data API research: Catalog.beer (candidate), beer.db (rejected); "auto-enrich first" principle

**Epic:** planning (feeds `epic:phone-experience`)

Researched two open beer-data projects the owner suggested, to fill the beer-level gap Open
Brewery DB can't (OBDB is breweries-only):

- **Catalog.beer (https://catalog.beer) — candidate.** ~60k beers / ~6.8k brewers, free
  under CC BY 4.0 (attribution line required). Beer object has exactly the beer-nerd
  fields the product wants: style (+ structured family and ale/lager class), ABV, IBU,
  description, nested brewer, `cb_verified` quality flags. REST API, key via free account
  (Basic auth), default 1,000 requests/month — fine with admin-add-time-only calls plus
  the same server-side caching planned for OBDB. **Gate before integrating:** a hit-rate
  spike searching a sample of the tavern's actual list (small local breweries are the
  likely misses); if the hit rate is poor, skip it.
- **beer.db / openbeer.github.io — rejected.** Right data shape on paper (public-domain
  beers with ABV/style), but dormant: most GitHub repos last touched 2015–2018, hosted
  API/admin was on Heroku's long-dead free tier. Stale data is useless for a rotating list.

**Product principle stated (owner direction):** customers get cool beer-nerd data (ABV,
IBU, style family, description, brewery provenance), and bartenders/owner should **not**
have to enter beer information — auto-enrich every field from open projects first, keep
manual admin entry as the always-available fallback and override. Also noted: the `Beer`
entity needs to grow (Abv, Ibu, style metadata, OBDB id, optional Catalog.beer id,
Availability) — plan the migration once. Open BJCP-style guideline datasets flagged as a
later candidate for per-style "what is a saison?" primers.

Docs updated: `TECHNICAL_ARCHITECTURE_PLAN.md` §6 (sourcing principle + both API verdicts),
`FEATURE_MAP.md`, `PROJECT_PLAN.md`, `MOBILE_FIRST_PRODUCT_OUTLINE.md`, `MVP_SCREEN_PLAN.md`,
`IMPLEMENTATION_BACKLOG.md` (spike + integration items), `EPICS_AND_SPRINTS.md`
(phone-experience scope), `PERSONAS_AND_USAGE.md` (Sam's add-a-beer flow, Dana's detail page).

No product code changed this session — docs only.

## 2026-07-14 — Feature addition: "My Beers" — completed list, ratings, want list, personal stats visualizations

**Epic:** planning (extends `epic:retention`)

Added the customer's personal layer over their confirmation history, per owner direction:

1. **My Beers** — the completed list: every confirmed beer with its date, searchable,
   sortable by date/name/style/my rating.
2. **Ratings ("rank your beers")** — 1–5 stars on any beer the customer has had confirmed
   (rating requires a confirmation, keeping rankings tied to club integrity). Prompted
   with "How was it?" on the PIN pad success screen; private by default. Ratings graduate
   out of the beer-journal bullet (journal keeps tasting notes).
3. **Want list** — the "not sure what to order" answer, superseding the old
   favorites/watchlists idea: add from search/detail, in-stock-tonight filter on by
   default, auto-check-off on confirmation, and an automated targeted push when a wanted
   beer flips to on-tap ("Beer X you wanted is on tap tonight").
4. **My Stats** — beer-nerd visualizations of completions + ratings: progress over time,
   style-family breakdown, explored-vs-remaining by style, ABV distribution, rating
   distribution + average by style. One `GET /api/me/stats` aggregate endpoint,
   lightweight client-side charts.
5. **Owner signals** — want-list demand counts + anonymized average ratings per beer on
   the owner dashboard (purchasing signals; want-count powers composer targeting).

Data model: `BeerRating` (unique per user+beer, requires confirmation) and `WantListItem`
(auto-resolved on confirmation, on-tap trigger through the §4.2 push pipeline) — see
`TECHNICAL_ARCHITECTURE_PLAN.md` §4.7 (new).

Docs updated: `TECHNICAL_ARCHITECTURE_PLAN.md` (§4.7), `FEATURE_MAP.md`, `PROJECT_PLAN.md`
(new Epic 3.6), `MOBILE_FIRST_PRODUCT_OUTLINE.md`, `MVP_SCREEN_PLAN.md` (three new
screens: My Beers / Want List / My Stats), `IMPLEMENTATION_BACKLOG.md` (Phase 6
sub-block), `EPICS_AND_SPRINTS.md`, `PERSONAS_AND_USAGE.md` (Dana's bar night + couch
loop, Terri's demand signals, new want-list cross-persona flow).

No product code changed this session — docs only. Committed and pushed to
`docs/phone-first-replan` per owner request (together with the 2026-07-13/14 planning
sessions' doc changes).

## 2026-07-14 — Sprint 1 implementation: mug-club core loop (issues #2–#6)

**Epic:** Mug Club Progress & Bartender Confirmation (`epic:mug-club`), Sprint 1

First product-code session of Sprint 1 — the whole core loop, built to the one-device
design merged in PR #10. Re-titled and re-scoped issues #3/#6 on GitHub first.

- **#2 Data model:** `Tavern`, `BeerConfirmation` (unique per customer+beer, FKs
  restrict), and `StaffPin` (hashed PIN, `IsActive`, lockout columns schema-ahead for
  Sprint 2). Migration `AddMugClubCore`; seed adds the tavern and a dev bartender
  (`bartender@example.com`, PIN `123456` — dev bootstrap only, real PIN lifecycle is
  Sprint 2/hardening scope).
- **#3 Confirm endpoint:** `POST /api/confirmations {beerId, pin}` authenticated as the
  *customer*; PIN resolves server-side to an active Bartender/Admin staff pin (Identity
  `PasswordHasher` verify) and stamps `ConfirmedByUserId`. 400 malformed PIN, 401
  generic "Invalid PIN.", 404 unknown beer, 409 already-confirmed, 201 with
  `{confirmedCount, goal, mugEarned}`.
- **#4 Progress endpoint:** `GET /api/me/progress` — count, goal (200), mugEarned, and
  the confirmed list (beer name/brewery/style + date, newest first).
- **#5 My Progress screen:** `/progress` route — X of 200, progress bar, mug-earned
  state, confirmed-beers list, sign-in prompt when logged out.
- **#6 Confirmation PIN Pad:** full-screen takeover from "Confirm with bartender" on
  beer detail (shown only when signed in): beer name large, masked digits-only 6-digit
  input, error + retry, success state with updated count and mug-earned celebration.
- **Tests (TDD Definition of Done):** 10 new backend tests (unit: ConfirmationsController
  incl. wrong/inactive/non-staff PIN and mug-earned threshold; MeController ordering and
  goal; integration: full register→confirm→progress loop over HTTP with the seeded PIN)
  — 37/37 green; 16 new frontend tests (api.js, ConfirmPinPad, MyProgress, BeerDetail
  confirm-button visibility) — 38/38 green.
- **Verified live** (not just tests): rebuilt the compose stack; `AddMugClubCore`
  applied onto the existing Postgres volume; drove the loop with curl — 401 no-token,
  401 wrong PIN, 400 malformed, 201 confirm, 409 duplicate, 200 progress. Captured a
  project verify skill at `.claude/skills/verify/SKILL.md` (includes the local .NET 8
  vs 10 gotcha: use `~/.dotnet8` for tests, not `DOTNET_ROLL_FORWARD`).

**Where this stands / resume here:**
- Branch `feat/sprint1-mug-club-core`, commit `aba0319`, pushed —
  [PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11) open to `master`,
  **CI green** (backend 43s, frontend 14s).
- **Next action: merge PR #11** (auto-closes issues #2–#6), close the Sprint 1 milestone,
  then groom Sprint 2 (Mug Club Completion) into GitHub issues: PIN lockout (the
  `FailedAttempts`/`LockedUntil` columns already exist on `StaffPin`, unused), PIN
  lifecycle in user management, "mug earned" notification, admin confirmation
  audit/correction with reason notes.
- To try it locally: `cd beer-app && docker compose up -d --build` → sign up at
  `localhost:3001`, open a beer, "Confirm with bartender", PIN `123456`.
- Tooling gotcha for next session: run backend tests with the .NET 8 SDK at `~/.dotnet8`,
  NOT `DOTNET_ROLL_FORWARD` — full recipe in `.claude/skills/verify/SKILL.md`.

## 2026-07-14 — Sprint 1 closed out; Sprint 2 groomed into issues #12–#16

**Sprint/story:** wraps Sprint 1 (Mug Club Core), opens Sprint 2 (Mug Club Completion) — `epic:mug-club`.

- Confirmed [PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11) merged to
  `master` (`3cf94c3`) — issues #2–#6 auto-closed, Sprint 1 milestone closed on GitHub.
  Verified the running Docker stack matches merged `master` (api image built from code
  byte-identical to the merge; web serves the working tree via the Vite volume mount).
- Created the **Sprint 2: Mug Club Completion**
  [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/2) and groomed the
  planned scope into five stories, per the "only the next sprint gets ticketed" rule:
  - [#12](https://github.com/pmconnolly80/FinalCapstone/issues/12) API: PIN lockout —
    per-PIN (`FailedAttempts`/`LockedUntil`, schema-ahead since Sprint 1) + per-customer
    axis; generic 401s so there's no lockout oracle
  - [#13](https://github.com/pmconnolly80/FinalCapstone/issues/13) PIN lifecycle — admin
    issue/reset/deactivate, staff change their own; uniqueness among active PINs
  - [#14](https://github.com/pmconnolly80/FinalCapstone/issues/14) Mug earned — persist
    `MugEarnedAtUtc` (durable, not derived), owner list of earners; push/badges deferred
    to the Retention epic
  - [#15](https://github.com/pmconnolly80/FinalCapstone/issues/15) API: admin
    confirmation audit & correction — required reason notes, audit log entity
    (also `epic:admin` — first slice of edit-everything)
  - [#16](https://github.com/pmconnolly80/FinalCapstone/issues/16) UI: admin confirmation
    review & correction screen (also `epic:admin`)
- Updated `EPICS_AND_SPRINTS.md` to mirror: Sprint 1 marked complete (stale re-scope
  warning removed — #3/#6 were re-titled on GitHub before implementation), Sprint 2
  section now links milestone + issues.

**Resume here:** implement Sprint 2, suggested order #12 → #13 → #14 → #15 → #16 (lockout
first — it hardens the already-live confirm endpoint; UI story depends on #15's API).
TDD per Definition of Done. Backend tests need the .NET 8 SDK at `~/.dotnet8` — recipe in
`.claude/skills/verify/SKILL.md`.

## 2026-07-14 — Live testing found three issues; triaged, two Sprint 2 interrupts filed

**Sprint/story:** Sprint 2 interrupts — `epic:auth` (bug #17) + `epic:phone-experience` (#18);
plus grooming/process updates.

Peter navigated the running app as a user and hit three problems: no social sign-in on the
auth screens, registration failing with no explanation, and a placeholder-grade landing page.
Triage outcome, one item per bucket:

- **Registration failure → [#17](https://github.com/pmconnolly80/FinalCapstone/issues/17)**
  (`bug` + `epic:auth`, pulled into the Sprint 2 milestone as an interrupt). Root cause
  diagnosed by static trace: `register()`/`login()` in `frontend/src/lib/api.js` throw a
  generic message without reading the response body, so the backend's real errors (Identity
  default password rules rejecting casual passwords; duplicate-email 409) never reach the
  user. CI never caught it because every auth test uses the compliant `Passw0rd!`. This also
  established the project's bug convention — new "Bugs" section in `EPICS_AND_SPRINTS.md`.
  **Not fixed this session** — it's first in the Sprint 2 working order.
- **Landing page → [#18](https://github.com/pmconnolly80/FinalCapstone/issues/18)**
  (`epic:phone-experience`, label created; Sprint 2 interrupt). Deliberately small scope:
  adopt Tailwind, extract `/` into a real `Home.jsx`, restyle the app shell. The full
  progress-as-home screen stays in the Customer Phone Experience sprint. Implemented this
  session (see branch `feat/landing-page-facelift`).
- **Social sign-in → no ticket.** Already a named later sprint (Auth II) under `epic:auth`;
  per the "only the next sprint gets ticketed" rule it stays prose-only. Epics table now
  notes the gap was re-confirmed by live testing.

Also: blog post "Live Testing and Triage" drafted for peterconnolly-website with the matching
Projects-page updates entry (same-commit rule).

**Resume here:** fix #17 first (it blocks account creation for all Sprint 2 testing), then
#12 → #13 → #14 → #15 → #16. TDD per Definition of Done. Backend tests need the .NET 8 SDK
at `~/.dotnet8` — recipe in `.claude/skills/verify/SKILL.md`.

## 2026-07-14 — #17 in progress: code complete on `fix/17-registration-errors`, live verify pending

**Sprint/story:** Sprint 2 interrupt [#17](https://github.com/pmconnolly80/FinalCapstone/issues/17) — `epic:auth`.

TDD done and both suites green (backend 41/41, frontend 45/45); session paused before
live verification due to usage limits. What's on the branch:

- Tests written first: 4 backend (`AuthControllerTests` — `beer1234` registers OK,
  `beer123` → 400 containing "at least 8 characters", duplicate email → 409 with message,
  wrong password → 401 "Invalid credentials.") and 5 frontend (api.test.js: register/login
  surface `body.message`; AuthPage.test.jsx: hint visible in register mode, short password
  blocked client-side without calling the API, API error message displayed).
- Implementation: `Program.cs` — explicit length-only password policy (min 8, all
  composition flags off; NIST-style, one explainable rule); `api.js` — `register()`/`login()`
  parse the error body like `confirmBeer()`; `AuthPage.jsx` — hint text
  "Passwords need at least 8 characters." in register mode + client-side length check
  ("Password is too short.").

**Resume here (remaining steps for #17):**
1. Live verify: `cd beer-app && docker compose up -d --build api web`, then curl
   `/api/auth/register` with `beer123` (expect 400 + message), `beer1234` + fresh email
   (expect 200 + token), same email twice (expect 409 + message). See
   `.claude/skills/verify/SKILL.md`.
2. Open PR from `fix/17-registration-errors` with "Closes #17", confirm CI green, merge.
3. Then Sprint 2 proper: #12 → #13 → #14 → #15 → #16.

## 2026-07-14 — #17 finished and merged; both Sprint 2 interrupts cleared

**Sprint/story:** Sprint 2 interrupt [#17](https://github.com/pmconnolly80/FinalCapstone/issues/17) — `epic:auth`.

Picked up from the previous entry's resume notes. Live verification against the rebuilt
Docker stack passed all four scenarios (`beer123` → 400 with the length message,
`beer1234` → 200 + token — the original silent failure, duplicate email → 409, wrong
login password → 401 "Invalid credentials.").
[PR #20](https://github.com/pmconnolly80/FinalCapstone/pull/20) merged with CI green
(backend 41/41, frontend 45/45); #17 auto-closed. Earlier the same day
[PR #19](https://github.com/pmconnolly80/FinalCapstone/pull/19) closed #18 (landing-page
facelift, Tailwind v4 adopted). Sprint 2: 2 of 7 done.

**Resume here:** Sprint 2 proper, in order: #12 (PIN lockout) → #13 → #14 → #15 → #16.
TDD per Definition of Done. Backend tests: .NET 8 SDK at `~/.dotnet8`
(`.claude/skills/verify/SKILL.md`). Note for future auth work: password policy is now
explicit length-only min 8 (`Program.cs`), kept in sync with the AuthPage hint.

## 2026-07-15 — Sprint 2 #12 shipped: PIN lockout on both axes

**Sprint/story:** [#12](https://github.com/pmconnolly80/FinalCapstone/issues/12) — `epic:mug-club`.

TDD: 6 new tests written first (5 controller unit + 1 HTTP flow), backend suite 47/47.
Per-PIN axis wires up the schema-ahead `FailedAttempts`/`LockedUntil` columns: a wrong
guess counts against every unlocked active PIN (a candidate is equidistant from all of
them), 5 consecutive failures lock a PIN for 15 min, expired locks reset lazily, success
resets that PIN. Per-customer axis is a new `FailedConfirmationAttempt` table
(`AddPinLockout` migration): 5 failures in a rolling 15-min window block the account
before PIN verification; a successful confirm clears the window. Every rejection is the
same generic 401 "Invalid PIN." — the attempt rows keep the real reason (wrong-pin /
pin-locked / customer-blocked) server-side. Live-verified against Docker (lock trips,
correct PIN rejected generically, reasons recorded); dev DB lock state reset afterwards.

**Resume here:** #13 (PIN lifecycle) → #14 → #15 → #16.

## 2026-07-15 — Sprint 2 #13 shipped: PIN lifecycle

**Sprint/story:** [#13](https://github.com/pmconnolly80/FinalCapstone/issues/13) — `epic:mug-club`.

TDD: 13 controller unit tests + 4 HTTP lifecycle/role tests + 6 frontend tests written
first; suites 65/65 backend, 51/51 frontend. New `StaffPinsController`:
`PUT /api/staff-pins/me` (Bartender/Admin change their own PIN),
`PUT /api/staff-pins/{userId}` (admin issue/reset), `DELETE /api/staff-pins/{userId}`
(admin deactivate). PINs hashed at rest, 6-digit validated, unique among *active* pins
(verified against each active hash; re-using your own is allowed); setting a PIN clears
lock state. Minimal staff "My PIN" screen at `/my-pin` (`MyPin.jsx`, Tailwind), customers
get the API's 403. Live-verified: own-PIN change flips the confirm flow old→new, customer
403, dev PIN restored to `123456`. Admin lifecycle is API-first per grooming — the
user-management table stays in the Admin Experience epic.

**Resume here:** #14 (durable mug-earned) → #15 → #16.

## 2026-07-15 — Sprint 2 #14 shipped: durable mug-earned milestone

**Sprint/story:** [#14](https://github.com/pmconnolly80/FinalCapstone/issues/14) — `epic:mug-club`.

TDD: new `MugAward` entity (`AddMugAward` migration, unique per customer) stamped exactly
once when the 200th confirmation lands; earned status now reads from the stored award
everywhere — never recomputed from the live count — so it survives catalog churn and
future admin corrections (#15). `ProgressResponse` gained `mugEarnedAt`; My Progress shows
the earned date; `GET /api/mug-awards` (Admin) lists earners oldest-first for the physical
mug handover. One pre-existing test updated to the new contract (at-goal count without an
award = not earned — that's the point of the story). Suites 73/73 backend, 51/51 frontend.
Live-verified: simulated 199 confirmations, landed the 200th through the real API, watched
the stamp; cleaned up after. Push/badges stay in the Retention epic per grooming.

**Resume here:** #15 (admin confirmation audit & correction API) → #16 (its screen).

## 2026-07-15 — Sprint 2 #15 shipped: admin confirmation audit & correction API

**Sprint/story:** [#15](https://github.com/pmconnolly80/FinalCapstone/issues/15) — `epic:mug-club` + `epic:admin`.

TDD: 8 unit + 3 HTTP tests written first; backend 85/85. New `AdminConfirmationsController`
(Admin-only): `GET /api/admin/confirmations` (filters: customer, bartender, beer, date
range), `GET .../audits`, `POST .../{id}/void` with a **required** reason. A void
hard-deletes the confirmation (freeing the customer+beer slot so the right beer can be
re-confirmed) and writes a `ConfirmationAudit` row in the same save — actor, timestamp,
reason, and the original record's data including the beer name at void time
(`AddConfirmationAudit` migration). **Mug-earned edge decided and documented in
`TECHNICAL_ARCHITECTURE_PLAN.md` §4.1: earned is permanent once stamped** — voids never
revoke the award. Live-verified end to end (400 without reason, void, progress drops to 0,
re-confirm 201, audit row readable, customer 403).

**Resume here:** #16 (the admin correction screen on this API) — last story in Sprint 2.

## 2026-07-15 — Sprint 2 #16 shipped: admin correction screen — sprint complete

**Sprint/story:** [#16](https://github.com/pmconnolly80/FinalCapstone/issues/16) — `epic:mug-club` + `epic:admin`.

TDD: 5 api-client tests + 6 RTL page tests written first; frontend 61/61. New
`/admin/confirmations` route (`AdminConfirmations.jsx`, Tailwind): filterable confirmation
history (client-side text filter over customer/bartender/beer), **two-step void guard**
(Void reveals a reason field + Confirm void; empty reason blocked client-side and 400
server-side), audit trail rendered in place (who voided, when, why), API errors surfaced.
Nav gets an "Admin" link only when the JWT carries the Admin role
(`getRolesFromToken()` in `api.js` — client convenience; the API enforces Admin
server-side regardless). **This closes Sprint 2: all 7 items done (#12–#16 + interrupts
#17/#18).**

**Resume here:** Sprint 2 close-out done — next sprint to groom: Customer Phone
Experience (see EPICS_AND_SPRINTS.md "Later sprints").

## 2026-07-15 — Sprint 2 close-out addendum

Post-close documentation sweep: sprint-close blog post published on peterconnolly-website
("Sprint 2 Closes and the Mug Club Epic Is Done", with the Projects-page updates entry in
the same commit per standing rule). `DIAGRAMS_AND_STORYBOARD.html` refreshed per its
snapshot rule — every sprint 1/sprint 2 element flipped to built (legend now single-state),
matching the closed milestone. All tracking docs verified current: `EPICS_AND_SPRINTS.md`,
`CLAUDE.md`, `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1, this log.

**Resume here:** groom the Customer Phone Experience sprint into issues.

## 2026-07-20 — Sprint 3 groomed: Customer Phone Experience

Broke the Customer Phone Experience epic into [milestone #3](https://github.com/pmconnolly80/FinalCapstone/milestone/3)
and 7 issues (#26–#32), mirroring the API/UI split style of Sprints 1–2: Beer.Availability
data model (#26) → search API (#27, depends on #26) → search-first list UI (#28, depends
on #27); beer-nerd stats + OBDB brewery card (#29) → admin OBDB autocomplete (#30, shares
#29's caching service); the Catalog.beer pre-fill spike (#31, go/no-go against the tavern's
real list); and the mobile UX repair bundle (#32: progress-centric home, auth-aware nav,
CRUD off the customer surface, hardcoded-localhost fix, loading/error states). No code
written this session — grooming only. `EPICS_AND_SPRINTS.md` updated with the new Sprint 3
section and epic status.

**Resume here:** #26 (Beer.Availability data model) — first story in Sprint 3, everything
else in the search slice depends on it.

## 2026-07-21 — Sprint 3 #26 shipped: Beer.Availability data model

**Sprint/story:** [#26](https://github.com/pmconnolly80/FinalCapstone/issues/26) — `epic:phone-experience`.

TDD: 3 new unit tests (model default, explicit-availability persistence) plus 1 integration
test (JSON contract) written first; backend 88/88. Added `BeerAvailability` enum (`OnTap`,
`Available`, `OutOfStock`, `Retired`) and `Beer.Availability` (defaults to `Available`),
`AddBeerAvailability` migration storing it as text via `HasConversion<string>` (legible
directly in the DB), backfilling existing rows to `Available`. Hit one wrinkle: registering
`JsonStringEnumConverter` globally in `Program.cs` only affects callers that use the
server's own `JsonOptions` — the integration test's plain `HttpClient` (representing any
external System.Text.Json consumer) doesn't inherit it and failed to parse the string back
into the enum. Fixed by putting `[JsonConverter(typeof(JsonStringEnumConverter))]` directly
on the enum instead, which is caller-agnostic; dropped the now-redundant `Program.cs`
registration. Live-verified against Docker: seed backfill correct, POST without an
`availability` field still defaults to `Available` (object-initializer semantics survive
System.Text.Json's partial-property deserialization), PUT round-trips an explicit value.

**Resume here:** #27 (beer search endpoint — name/brewery/style, pagination, availability +
had/not-had filters) — depends on #26.

## 2026-07-21 — Sprint 3 #27 shipped: beer search endpoint

**Sprint/story:** [#27](https://github.com/pmconnolly80/FinalCapstone/issues/27) — `epic:phone-experience`.

TDD: 13 new unit tests (search/availability/hadStatus/pagination/confirmed-flag
combinations) plus 2 integration tests written first; backend 101/101. `GET /api/beers`
is now the search endpoint: `search` (case-insensitive substring over name/brewery/style),
`availability` (a specific state, or `all`; omitted defaults to in-stock —
`OnTap`/`Available` only, since the rotating inventory shouldn't surface out-of-stock/
retired beers by default), `hadStatus` (`had`/`nothad`, computed against the authenticated
customer's `BeerConfirmations` — 401 if no token), `page`/`pageSize` (default 200, since
the tavern's real catalog runs to ~200 beers and this keeps today's UI on one page without
behavior change). Response is a `BeerSearchResponse` envelope (`items`, `page`, `pageSize`,
`totalCount`); each item carries a `confirmed` flag for the calling customer (false when
anonymous) — #28's list screen needs this for its confirmed-checkmark requirement, not just
the hadStatus filter.

Design choice worth flagging: this changes `GET /api/beers`'s response shape from a bare
array to the envelope, which breaks existing consumers. Rather than touch `BeerList.jsx`'s
actual UI (that's #28's job — "rebuild the beer list screen"), `fetchBeers()` in `api.js`
now unwraps `.items` so `BeerList.jsx` keeps rendering exactly as before; three integration
test helpers (`AdminConfirmationsTests`, `ConfirmationsFlowTests`, `StaffPinLifecycleTests`)
were updated the same way. Backend 101/101, frontend 61/61, live-verified against Docker:
search, availability filter (including the 400 on garbage input), hadStatus's 401-when-
anonymous and real confirm-then-filter round trip, and pagination.

**Resume here:** #28 (search-first beer list UI) — depends on #27, the last piece before
moving to #29 (beer-nerd stats + OBDB brewery card).

## 2026-07-21 — Sprint 3 #28 shipped: search-first beer list UI

**Sprint/story:** [#28](https://github.com/pmconnolly80/FinalCapstone/issues/28) — `epic:phone-experience`.

TDD: 4 new `api.js` tests (`searchBeers` query-string building, auth header, envelope
passthrough) plus 9 new `BeerList` RTL tests written first; frontend 70/70. `fetchBeers()`
replaced with `searchBeers(params)`, returning the full `{items, page, pageSize,
totalCount}` envelope rather than unwrapping it — this page needed `totalCount` and the
per-item fields anyway, so the temporary unwrap from #27 was removed rather than kept.
`BeerList.jsx` rebuilt on Tailwind (the last inline-styled beer-facing page — matches the
"restyled once Customer Phone Experience lands" note in `CLAUDE.md`): debounced (300ms)
search-as-you-type, an availability chip row (In Stock/On Tap/Available/Out of Stock/
Retired/All), a had/not-had chip row shown only when signed in (since `hadStatus` 401s
anonymously per #27), and style/brewery "quick-search" chips computed from the current
result page. Design note: the search API has one combined free-text field spanning name/
brewery/style, not separate structured style/brewery params — so those chips fill the
search box rather than compose as independent filters; genuinely composable are search
text + availability + hadStatus, which the API does support together. Each result shows an
availability badge and a confirmed checkmark.

Side quest: live-verifying against Docker hit a stale-cache surprise — `docker compose up
-d --build web` without rebuilding still served pre-edit `api.js`. Traced it to
`docker-compose.yml` having no volume mount for `web`; the `verify` skill claimed otherwise
(carried over from an earlier compose setup, presumably). Fixed the skill doc so this
doesn't cost a debugging session again. Backend untouched (101/101 still green), frontend
70/70, live-verified: dev-server module content, field-casing match between the API
response and what the component reads.

**Resume here:** #29 (beer-nerd stats + Open Brewery DB brewery card) — next in Sprint 3.

## 2026-07-21 — Sprint 3 #29 shipped: beer-nerd stats + Open Brewery DB brewery card

**Sprint/story:** [#29](https://github.com/pmconnolly80/FinalCapstone/issues/29) — `epic:phone-experience`.

TDD: 5 `OpenBreweryDbService` tests (success mapping, cache-hit avoids a refetch, 404 →
null, network exception → null, failures aren't cached so they retry) + 4 new
`BeersController.GetBeer` tests, written first; backend 110/110. `Beer` grows `Abv`
(double?), `Ibu` (int?), `StyleFamily` (string?), and `Class` (nullable `BeerClass` enum,
`Ale`/`Lager`, stored as text like `Availability`) plus `ObdbBreweryId` (string?) —
`AddBeerNerdStatsAndObdbBreweryId` migration.

New `beer-app/backend/Services/` — the backend's first external-API integration. A hand-
rolled `FakeHttpMessageHandler` test double stands in for OBDB (no Moq in this project);
`OpenBreweryDbService` wraps a typed `HttpClient` (`AddHttpClient<IBreweryLookupService,
OpenBreweryDbService>`) and an `IMemoryCache` (24h TTL — TECHNICAL_ARCHITECTURE_PLAN.md §6
calls caching mandatory, "stale data is fine, breweries rarely move"). Any failure —
404, network down, malformed JSON — returns `null` rather than throwing, so a bad or
unreachable OBDB record never breaks beer detail; failures also aren't cached, so a
transient outage self-heals on the next request instead of being pinned for 24h.

`GET /api/beers/{id}` now returns a `BeerDetailResponse` (nerd-stat fields + a resolved
`BreweryInfo?`) instead of the raw `Beer` entity — `PostBeer`/`PutBeer` stay on the raw
entity untouched, so this is additive for the write path. `BeerDetail.jsx` renders a
nerd-stats block and brewery card (with a website link) only when the corresponding data
is present; `BeerForm.jsx` gained ABV/IBU/style-family/class inputs, with the submit path
converting blank number inputs to `null` rather than an empty string (which would fail to
bind to the nullable `double`/`int` properties). `ObdbBreweryId` has no form control yet —
admins set it via raw PUT until #30's autocomplete. Frontend 77/77.

Live-verified against the *real* Open Brewery DB API (not a stub): looked up Sierra
Nevada's Chico, CA brewery record by id, confirmed the resolved brewery card, confirmed a
second fetch was cache-served, and confirmed an invalid brewery id degrades to
`breweryInfo: null` without a 500.

**Resume here:** #30 (admin Open Brewery DB brewery autocomplete) — reuses #29's caching
service, wires the missing piece (setting `ObdbBreweryId` from the admin UI instead of raw
PUT).

## 2026-07-21 — Sprint 3 #30 shipped: admin Open Brewery DB brewery autocomplete

**Sprint/story:** [#30](https://github.com/pmconnolly80/FinalCapstone/issues/30) — `epic:phone-experience`.

TDD: 5 new `OpenBreweryDbService.SearchBreweriesAsync` tests (mapped results, cache-by-
query avoids a refetch, empty on unreachable OBDB, empty on blank query without a network
call) + 2 `BreweriesController` unit tests + 3 HTTP-level role-gating tests (401 anonymous,
403 customer, 200 admin — same pattern as `BeersAuthorizationTests`), written first;
backend 119/119. `IBreweryLookupService` gained `SearchBreweriesAsync(query)`, hitting
OBDB's `breweries/search?query=` and caching by the normalized query string in the same
`IMemoryCache` from #29 — failures return an empty list rather than throwing, same
resilience posture as the single-brewery lookup. New `[Authorize(Roles = "Admin")]
BreweriesController` at `GET /api/breweries/search` — admin-only since this endpoint has no
customer-facing use.

`BeerForm.jsx`'s Brewery field is now a debounced (300ms) autocomplete: typing shows a
suggestion dropdown (name + city/state) below the field; selecting one fills the field and
stores `ObdbBreweryId`. Design choice for the "manual entry stays the fallback/override"
acceptance criterion: typing into the field by hand — whether from scratch or editing after
a selection — clears the stored `ObdbBreweryId`, so it's never possible for a stale link to
survive an edit the admin didn't explicitly re-confirm via another selection. `searchBreweries(query)`
added to `api.js`. Frontend 81/81 (2 new tests for select-fills-and-stores and
edit-after-select-clears-id).

Backend hit one C# compiler wrinkle: `ActionResult<IReadOnlyList<BreweryInfo>>` refused to
implicitly convert from a same-typed `IReadOnlyList<BreweryInfo>` local (a genuine operator-
resolution quirk, not a caching or nullability issue) — sidestepped by dropping the
`ActionResult<T>` wrapper entirely, since this action never returns an alternate status from
its body (auth failures are handled by the `[Authorize]` middleware, not action code).

Live-verified against Docker with the *real* OBDB search API: role gating (401/403/200),
a live "sierra nevada" search returning real brewery records, cache-hit timing, and the
blank-query short-circuit never hitting the network.

**Resume here:** #31 (Catalog.beer beer-level pre-fill spike) — the hit-rate spike against
the tavern's real list, go/no-go decision, then (if go) admin pre-fill wiring.

## 2026-07-21 — Sprint 3 #31 shipped: Catalog.beer pre-fill spike — GO, integration wired

**Sprint/story:** [#31](https://github.com/pmconnolly80/FinalCapstone/issues/31) — `epic:phone-experience`.

The spike needed a real Catalog.beer API key, which requires an account with email
verification — not something obtainable autonomously. Asked the user; they signed up and
provided the key. **The key never touched a committed file at any point**: it's read from
`CatalogBeer:ApiKey` config (empty string in the committed `appsettings.json`),
overridable via `CatalogBeer__ApiKey` in `docker-compose.yml` from `${CATALOG_BEER_API_KEY}`,
sourced from a new `beer-app/.env` — added `.env`/`.env.*` to `.gitignore` *before* creating
that file, and double-checked with `git check-ignore -v` and `git status` before doing
anything else with it.

Ran the real API (via a throwaway shell variable, never a script argument that could land
in shell history review or a file) against the 8 beers in the seeded catalog, searching
"name + brewery" combined (matching how an admin would naturally search). Result:
**6/8 clear hits, 1/8 close** (Weihenstephaner's Hefeweizen is filed as "Hefeweissbier" —
a recognizable synonym), **1/8 miss** (Samuel Smith's Oatmeal Stout — brewery present,
that specific beer isn't). Notably the misses were well-known European breweries, not the
"small/local" ones `TECHNICAL_ARCHITECTURE_PLAN.md` §6 had predicted — logged as a
correction to that prediction. `cb_verified` was `false` on every result in this sample;
recorded as a signal to weight lightly, not gate on. **Decision: GO** — documented in
§6 with the full per-beer breakdown.

Per the story's acceptance criteria ("if go: pre-fill wired... attribution present"), built
the integration in the same story rather than a follow-up: TDD, 7 `CatalogBeerService`
tests (mapped fields, `cb_verified`-first sort, Basic-auth header construction, caching by
query, graceful failure/unreachable/no-key/blank-query handling) + 2 controller tests + 3
HTTP role-gating tests, all written first; backend 131/131. `ICatalogBeerService`/
`CatalogBeerService` mirrors #29/#30's OBDB pattern (typed `HttpClient`, `IMemoryCache`,
never throws) but needs the API key at request time rather than HttpClient-creation time,
so the Basic auth header is built per-request from `IConfiguration` rather than set as a
`HttpClient` default header. New Admin-only `CatalogBeerController` at
`GET /api/catalog-beer/search`.

`BeerForm.jsx`'s Name field is now a second debounced autocomplete (independent of the
Brewery/OBDB one from #30): selecting a Catalog.beer suggestion pre-fills style, ABV, IBU,
style family, class, and description — mapping `class`/`parent` straight onto the
`Class`/`StyleFamily` fields #29 added, a nice fit with existing scope — and shows a CC BY
4.0 attribution line. Frontend 84/84 (2 new tests: pre-fill-and-attribution, plus the
existing suite untouched). Live-verified against Docker with the real key: role gating
(401/403/200), a live "duvel" search returning real pre-fill data, cache-hit timing, and
the blank-query short-circuit.

**Resume here:** #32 (mobile UX repair bundle) — the last story in Sprint 3: progress-
centric home, auth-aware nav, CRUD off the customer surface, hardcoded-localhost fix,
loading/error states.

## 2026-07-21 — Sprint 3 #32 shipped: mobile UX repair — Sprint 3 closed

**Sprint/story:** [#32](https://github.com/pmconnolly80/FinalCapstone/issues/32) — `epic:phone-experience`. Last story in Sprint 3.

TDD throughout: new `App.test.jsx` (5 tests), 5 new `Home.test.jsx` tests (replacing the
2 that assumed a single signed-out-only experience), 1 new `BeerDetail.test.jsx` test, and
6 new `BeerForm.test.jsx` tests (error message, edit-load error, required fields, plus 2
admin-gate tests), all written first; frontend 99/99 (was 84/84).

Six independent fixes bundled per the issue:

- **Hardcoded API URL**: `api.js`'s `API_BASE_URL` now falls back to
  `${window.location.protocol}//${window.location.hostname}:5153` instead of a literal
  `http://localhost:5153`, and `docker-compose.yml` no longer overrides it with that same
  literal — a phone opening the app at the host machine's LAN IP now reaches the API there
  instead of at itself. Live-verified over the host's actual LAN IP, not just `localhost`.
- **Auth-aware, reactive nav**: same-tab login/register/logout previously left `App.jsx`'s
  nav stale until a manual reload, since it read `getRolesFromToken()` once at initial
  render with no subscription to anything. New `AUTH_CHANGED_EVENT`/`logout()` in `api.js`
  — a `window` custom event dispatched on any auth change (the browser's own `storage`
  event only fires in *other* tabs) — that `App.jsx` now listens for for. Nav shows "Sign
  out" once signed in and gates "Add Beer" to Admins.
- **Progress-centric home**: `Home.jsx` fetches and renders the signed-in customer's actual
  X-of-200 progress + mug-earned state (same data shape as `MyProgress.jsx`) instead of
  always showing the generic mug-club pitch; anonymous visitors still see that pitch
  unchanged.
- **Beer CRUD actually off the customer surface**: hiding "Add Beer" from nav wasn't
  enough — a customer who typed `/beers/new` directly still saw a live (if uselessly
  fail-server-side) form. `BeerForm.jsx` now gates its own content the same way
  `AdminConfirmations.jsx`/`MyPin.jsx` already do, rendering an "admin account required"
  message for non-admins instead of the form.
- **Visible error states**: `BeerForm.jsx`'s edit-mode load failure and save failure, and
  `BeerDetail.jsx`'s load failure, were the last `console.error`-only paths in the app
  (`BeerList.jsx`/`MyProgress.jsx` already surfaced errors from earlier stories) — now all
  show a message the customer can actually see.
- **Form usability**: Name/Brewery/Style are `required`; `AuthPage.jsx`'s email/password
  inputs gained `type="email"` and `autoComplete` hints, plus real `<label>` elements
  instead of placeholder-only.

Live-verified against Docker: dynamic hostname derivation confirmed served, both the web
page and the API reachable over the actual LAN IP (not just `localhost`), a full register/
login round trip, and the new admin-gate/error-state code paths confirmed present in the
served bundle. Backend untouched (131/131 still green).

**This closes Sprint 3 — the Customer Phone Experience epic is done** (issues #26–#32,
PRs #33–#39, groomed 2026-07-20, closed 2026-07-21). Suites at close: backend 131/131,
frontend 99/99.

**Resume here:** groom the next named sprint into issues — **Auth II: Social Sign-in**
(Google/Facebook/Apple, account linking, marketing consent, privacy/data-deletion, and
password reset) is next per the "only the next sprint gets ticketed" rule.

## 2026-07-21 — Sprint 4 groomed: Auth II

Broke Auth II into [milestone #4](https://github.com/pmconnolly80/FinalCapstone/milestone/4)
and 7 issues (#40–#46), mirroring the API/UI split style of prior sprints:
`ApplicationUser` + marketing-consent data model (#40, foundational) → pluggable email
sender (#41) → forgot/reset password (#42, depends on #41); Google/Facebook/Apple
external sign-in (#43/#44/#45, independent of each other — #44 bundles the privacy
policy page and data-deletion path since Facebook's app review requires both) → social
buttons + account-linking screen + consent checkbox (#46, depends on #40/#43/#44/#45).
Approach was already decided in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6 (Identity external
login providers, not a hosted vendor; link-or-create by verified email; API keeps
issuing its own JWT) — this session's research (an Explore agent reading that section,
`IMPLEMENTATION_BACKLOG.md` Phase 3, and the current `AuthController.cs`/`Program.cs`)
confirmed no `ApplicationUser` class exists yet and all the OAuth/email-sender packages
are greenfield additions. No code written this session — grooming only.
`EPICS_AND_SPRINTS.md` updated with the new Sprint 4 section and the Auth & Roles epic
status; `CLAUDE.md`'s "Likely next steps" updated to the new story order.

**Process note:** these grooming doc updates were committed and pushed standalone at the
user's request, rather than bundled into the first story's PR (the pattern every prior
sprint's grooming followed — see #26's commit, which carried both the Sprint 3 grooming
docs and its own implementation). No functional difference; just flagging the deviation so
it doesn't read as an oversight later.

**Resume here:** #40 (`ApplicationUser` + marketing-consent migration) — first story in
Sprint 4, foundational for #42's consent capture and #46's UI.
