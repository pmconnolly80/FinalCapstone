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

---

## 2026-07-23 — Sprint 5 groomed: Admin Experience

**Epic:** `epic:admin`

Sprint 4 (Auth II, #40–#46, PRs #47–#52) closed 2026-07-23 with both test suites green
(backend 171/171, frontend 117/117). Per the "only the next sprint gets ticketed" rule,
groomed **Sprint 5: Admin Experience** into
[milestone #5](https://github.com/pmconnolly80/FinalCapstone/milestone/5) and 7 issues
(#53–#59), reading `FEATURE_MAP.md`'s Administration section, `IMPLEMENTATION_BACKLOG.md`
Phase 5, and `MVP_SCREEN_PLAN.md`'s laptop admin screens, plus the existing
`AdminConfirmationsController`/`StaffPinsController` code to scope what's already built
(Sprint 2's confirmation audit/void and PIN issue/reset/deactivate API) versus what's
still missing (any UI in front of PIN management, role assignment at all, and an audit
trail on beer edits).

Story order: #53 (`AdminAudit` trail generalizing Sprint 2's `ConfirmationAudit` pattern,
plus role assignment) is foundational → #54/#55 (user management API/UI) and #56/#57
(audited beer edit/delete + inline availability, Beer Management Table) build on it in
parallel → #58 (anomaly detection: bulk beer-add, confirmation velocity, off-hours,
informational only) is independent → #59 (Admin Dashboard) ties everything together and
closes the sprint.

Also closed out milestones #3 and #4 on GitHub, which had stayed open despite both
sprints being marked complete in the docs since 2026-07-21/07-23 — a bookkeeping gap,
not a scope change.

No code written this session — grooming only. `EPICS_AND_SPRINTS.md` updated with the
new Sprint 5 section and the Admin Experience epic status; `CLAUDE.md`'s "Likely next
steps" updated to the new story order.

**Resume here:** #53 (`AdminAudit` trail + role assignment API) — first story in
Sprint 5, foundational for #54/#55's account actions and #56/#57's audited beer edits.

---

## 2026-07-23 — #53: generalized AdminAudit trail + role assignment API

**Epic:** `epic:admin`

First story of Sprint 5. Added `AdminAudit` (`beer-app/backend/Models/AdminAudit.cs`,
`AddAdminAudit` migration) mirroring Sprint 2's `ConfirmationAudit` shape — actor,
entity type/id, action, before/after snapshots, required reason, timestamp — additive
to (not a replacement for) confirmations' existing audit trail. New
`AdminUsersController` (`[Authorize(Roles = "Admin")]`) exposes
`PUT /api/admin/users/{id}/role`: rejects a missing reason or an unrecognized role,
replaces the target user's existing role(s) via `UserManager`/`RoleManager` (matching
the app's single-role-per-user model), and writes the `AdminAudit` row in the same
save. Followed the repo's TDD policy: unit tests
(`BeerApi.Tests/Controllers/AdminUsersControllerTests.cs`, resolving `UserManager`/
`RoleManager` from a small DI container rather than hand-constructing them) and
integration tests (`BeerApi.Tests/IntegrationTests/AdminUsersTests.cs`, the usual
401/403/204/404 gating plus a full round trip) written alongside the implementation.
Suite green at 181/181 (171 prior + 10 new).

- Branch: `feat/53-admin-audit-role-assignment`
- PR: [#60](https://github.com/pmconnolly80/FinalCapstone/pull/60) (open, not yet merged)

**Resume here:** #54 (API: user management + account actions) once #60 merges —
builds on this story's `AdminAudit`/role-assignment work.

---

## 2026-07-23 — #54: user management + account actions API

**Epic:** `epic:admin`

Second story of Sprint 5, built on #53's still-open branch since it depends on that
work. Extended `AdminUsersController` with `GET /api/admin/users` (role, active/locked
status, and staff-PIN presence per user — a single batched `UserRoles`/`Roles` join for
role, reusing `StaffPin.IsActive` rather than duplicating it, avoiding per-user round
trips) and reversible `POST /api/admin/users/{id}/deactivate` /
`.../reactivate`. Deactivation piggybacks on ASP.NET Identity's own lockout mechanism
(`LockoutEnd`/`LockoutEnabled`) instead of a bespoke flag; discovered along the way that
`AuthController.Login` builds its own JWT rather than going through `SignInManager`, so
nothing was actually checking lockout status — added an `IsLockedOutAsync` check there
so deactivation isn't a silent no-op. Per `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1,
deactivating a bartender/admin also flips their `StaffPin.IsActive` to `false` in the
same request; reactivating deliberately does not restore the PIN. Both actions require
a reason and write an `AdminAudit` row, same pattern as #53. TDD as usual: unit tests
extending `AdminUsersControllerTests` and integration tests extending `AdminUsersTests`
(gating, validation, and a full deactivate → blocked login → reactivate → restored
login flow). Suite green at 198/198 (181 prior + 17 new).

- Branch: `feat/54-admin-user-management` (stacked on `feat/53-admin-audit-role-assignment`)
- PR: [#61](https://github.com/pmconnolly80/FinalCapstone/pull/61) (open, based on #60's
  branch, not yet merged)

**Resume here:** #55 (UI: User Management screen) once #60/#61 merge — wires up #53's
role assignment and #54's user list/deactivate/reactivate endpoints.

---

## 2026-07-23 — #60/#61 merged; #55: User Management screen

**Epic:** `epic:admin`

Merged PR #60 (#53) to `master`, retargeted PR #61 (#54) from #60's branch to `master`,
merged that too — both green in CI. Then built #55, the third story of Sprint 5: new
`AdminUsers.jsx` at `/admin/users` (nav entry, admin-gated), following
`AdminConfirmations.jsx`'s exact gate → load → table → two-step reason-guarded action
shape. A `<select>` per row wires role changes to #53's endpoint; Deactivate/Reactivate
buttons wire to #54's; new Set PIN/Deactivate PIN actions (staff rows only) wire up
Sprint 2's `StaffPinsController`, which never had any admin UI in front of it at all
until now. 6 new `src/lib/api.js` functions, all colocated tests (Vitest + RTL).

Manual end-to-end verification via `docker compose` + curl (no browser automation
available) caught a real bug: `AdminUsersController.GetUsers` built its per-user role
lookup with `ToDictionaryAsync`, which throws — 500ing the whole list — if any user
ever has more than one role row. The app's own `AssignRole` never produces that state,
but a manual DB correction could, and the verification flow's own admin-bootstrap step
(promoting a test user to Admin via raw SQL) hit exactly that. Fixed with a
`GroupBy`-then-`ToDictionary` that just picks one role, plus a regression test. Not a
regression from #54 — just never exercised until a real multi-role row existed.

Full flow verified live: role assignment (reason required), PIN issue, deactivate
(reason required, drops the PIN, blocks login with a clear message), reactivate
(restores login, PIN stays off). Suites green: backend 199/199 (198 prior + 1 bug-fix
regression test), frontend 131/131 (117 prior + 14 new).

- Branch: `feat/55-admin-user-management-ui` (off `master`, since #53/#54 are merged —
  no stacking needed this time)
- PR: [#62](https://github.com/pmconnolly80/FinalCapstone/pull/62) (open, not yet merged)

**Resume here:** #56 (API: audited beer edit/delete + inline availability update) once
#62 merges — builds on #53's `AdminAudit`.

---

## 2026-07-23 — #62 merged; #56: audited beer edit/delete + inline availability

**Epic:** `epic:admin`

Merged PR #62 (#55) to `master`. Then built #56, the fourth story of Sprint 5:
`BeersController`'s existing `PUT`/`DELETE` (already `[Authorize(Roles = "Admin")]`, but
previously changeable with zero audit trail) now write an `AdminAudit` row per edit/
delete, plus a new `PATCH /api/beers/{id}/availability` for the inline toggle #57's Beer
Management Table needs.

Format decision, confirmed with the user before implementing: #53/#54's
`BeforeSnapshot`/`AfterSnapshot` only ever held short single-value strings (a role name,
"Active"/"Deactivated") since each audited one scalar. A beer edit touches ~10 fields.
Rather than introduce this codebase's first JSON-blob snapshot, went with a
**changed-fields-only text diff** (a new `DescribeBeerDiff` helper) — only what actually
changed, as readable text, matching the existing plain-string tone. A no-op edit
(identical resubmission) writes no audit row. Per the issue, only delete requires an
admin-supplied reason; edits and availability flips are audited automatically without
one.

Along the way, `PutBeer` gained a proper `404` for an unknown id (previously threw
`DbUpdateConcurrencyException` from `SaveChangesAsync` — fetching the existing row for
diffing makes the check free) and `DeleteBeer` gained a required `reason` query
parameter (no existing frontend call to break, since beer deletion isn't wired up in
the UI yet — that's #57's job).

Manual end-to-end verification via `docker compose` + curl: created a beer, edited
style/ABV (confirmed the diff-only audit text via `psql`), re-submitted identical values
(confirmed no second audit row), flipped availability via PATCH, deleted without a
reason (400) then with one (204) — all four audit rows correct.

Suites green: backend 215/215 (199 prior + 16 new/updated). No frontend changes (API
only, matching #53/#54's split from #55).

- Branch: `feat/56-audited-beer-edit-delete` (off `master`)
- PR: [#63](https://github.com/pmconnolly80/FinalCapstone/pull/63) (open, not yet merged)

**Resume here:** #57 (UI: Beer Management Table) once #63 merges — wires up #56's
audited edit/delete/availability endpoints.

---

## 2026-07-23 — #63 merged; #57: Beer Management Table

**Epic:** `epic:admin`

Merged PR #63 (#56) to `master`. Then built #57, the fifth story of Sprint 5 and the
last one this session: new `AdminBeers.jsx` at `/admin/beers` ("Manage Beers" nav
entry), the admin-only table `MVP_SCREEN_PLAN.md` calls "the only place catalog CRUD
appears."

Investigation before implementing found the actual scope narrower than the issue title
suggests: `BeerList.jsx` (the customer-facing list) has no CRUD markup at all to
remove — the one real "customer-surface remnant" was the admin-gated "Add Beer" link
sitting in the main nav bar next to "Beers" rather than under the admin section.
Removed that, added the new admin nav entry instead. `BeerForm.jsx` is reused entirely
unchanged for Add/Edit per the issue (its OBDB autocomplete and Catalog.beer pre-fill
untouched) — only its post-save redirect moved from `/beers` to `/admin/beers` so the
admin round-trips to the new table instead of the customer list.

Availability changes fire immediately on the inline `<select>` (#56's PATCH endpoint
needs no reason); Delete is the one action gated behind a reason, reusing #55's
`AdminUsers.jsx` two-step `pendingAction` guard pattern exactly. 2 new `api.js`
functions (`updateBeerAvailability`, `deleteBeer` — the latter puts its reason in the
query string, matching how #56's backend binds it, unlike the POST+JSON-body reason
pattern `deactivateAccount`/`voidConfirmation` use).

4 existing tests needed updating rather than just adding new ones: `App.test.jsx`'s
"Add Beer" nav assertions became "Manage Beers" assertions (same hidden/shown-per-role
logic), and `BeerForm.test.jsx`'s post-save redirect test target changed from a
`/beers` placeholder route to `/admin/beers`.

Verified live: rebuilt `web`, confirmed the dev server serves the new page and the nav
no longer shows "Add Beer" but does show "Manage Beers" wired to `/admin/beers`;
confirmed `GET /api/beers?availability=all` (what the page's search call relies on)
returns the full catalog including out-of-stock/retired beers, not just in-stock.

Suites green: frontend 140/140 (131 prior + 9 new). No backend changes.

- Branch: `feat/57-beer-management-table-ui` (off `master`)
- PR: [#64](https://github.com/pmconnolly80/FinalCapstone/pull/64) (open, not yet merged)

**Resume here:** #58 (API: anomaly detection) once #64 merges — independent of the
beer/user work, surfaced by #59's Admin Dashboard which closes the sprint.

---

## 2026-07-23 — #64 merged; #58: anomaly detection (with a planning-process fix)

**Epic:** `epic:admin`

Merged PR #64 (#57) to `master`. Then planned and built #58 — asked explicitly to
review the plan against the project docs and look for issues before implementing, so
this one got an extra pass before coding started.

Findings from that review, each addressed in the design:
- **Bulk-add attribution gap**: neither `Beer` nor any audit table recorded who created
  a beer — #56 only audited `PUT`/`DELETE`. Since the whole point of the bulk-add
  anomaly is flagging a possibly-compromised admin account, an un-attributable burst
  is much less useful. Confirmed with the user: extended `PostBeer` to write an
  `AdminAudit` row too (`Action = "Create"`).
- **Test-time flakiness**: bucket-boundary and off-hours logic depending on
  `DateTime.UtcNow` would make unit tests wall-clock-dependent. Each of the three
  detection methods (`DetectBulkBeerAddAsync`/`DetectConfirmationVelocityAsync`/
  `DetectOffHoursActivityAsync` on the new `AdminAnomaliesController`) is `public
  static` and takes an explicit `DateTime now` parameter instead.
- **Timezone/wraparound**: `ConfirmedAt` is UTC but "off-hours" is local time for one
  physical tavern; config gets an optional `TimeZoneId`. Tavern hours can span midnight
  (open 10am, close 2am), so the in-hours check handles `CloseHour <= OpenHour` as a
  wrap rather than a plain range.
- **Velocity noise floor**: added a `MinimumCount` so a near-zero baseline doesn't trip
  the multiplier on 1-2 confirmations.
- **A real bug caught in the plan itself, not just the code**: re-reviewing the draft
  plan before implementing found a broken, unlabeled pseudocode sketch (a
  `.Cast<IGrouping<string?, dynamic>>().Append(null)` fragment) sitting in the same
  code-fence style as the real logic around it, plus a separate `int.Parse(a.EntityId)`
  inside an EF LINQ predicate that would have thrown at runtime (EF Core can't
  translate `int.Parse`). Both fixed before writing any code. Per explicit request,
  **`CLAUDE.md` gained a new "Planning conventions" section**: pseudocode/sketches in
  plan files or session log entries must be labeled as such, never presented like
  finished code.

Thresholds live in a new `Anomalies` config section (`appsettings.json` +
`docker-compose.yml` overrides), same `IConfiguration`-direct-read pattern as
`CatalogBeer`/`Email`. Verified live: a real burst of 10 beers correctly fired a
`BulkBeerAdd` anomaly attributed to the actual admin account that created them.

Suites green: backend 229/229 (215 prior + 14 new). No frontend changes — that's #59.

- Branch: `feat/58-anomaly-detection` (off `master`)
- PR: [#65](https://github.com/pmconnolly80/FinalCapstone/pull/65) (open, not yet merged)

**Resume here:** #59 (UI: Admin Dashboard) once #65 merges — surfaces #58's anomaly
feed and closes Sprint 5.

---

## 2026-07-23 — #65 merged; #59: Admin Dashboard — closes Sprint 5

**Epic:** `epic:admin`

Merged PR #65 (#58) to `master`. Then planned and built #59, the sixth and closing
story of Sprint 5 — asked again to plan it out and look for issues before coding.

Two real product/UX decisions surfaced during investigation, both resolved with the
user before designing further:
- **"Active members" had no fixed definition in the issue text.** This codebase's own
  `PERSONAS_AND_USAGE.md` defines "active member" as engagement-based (confirmed a beer
  recently), explicitly contrasted with "lapsed members" — not an account-status flag.
  Decision: implement the real definition (distinct customers with ≥1 confirmation in
  the last 30 days), not the cheaper "non-deactivated Customer account" reading.
- **"Becomes the landing page for the Admin role" wasn't true** — `Home.jsx` had zero
  role-awareness; every signed-in user, admins included, saw the customer progress
  card. Decision: `Home.jsx` now redirects Admin-role users to `/admin/dashboard` on
  load, matching the issue's literal wording, rather than just adding a nav link.

Neither of the four summary numbers had a cheap existing endpoint (no count-only path
on `GetConfirmations`, "active members" wasn't computable from anywhere at all), so
this added one new backend endpoint, `GET /api/admin/dashboard/summary`
(`AdminDashboardController`), returning all four as real `COUNT`/`COUNT(DISTINCT ...)`
queries in one round trip — cleaner than stitching together several imprecise
client-side counts. Same `public static` + explicit `DateTime now` testability pattern
established in #58, for the same reason (deterministic "today"/"last 30 days"
boundaries regardless of when tests run).

New `AdminDashboard.jsx` at `/admin/dashboard`: summary cards, an anomaly panel
rendering #58's `GET /api/admin/anomalies` (each item's `DeepLink` used directly as a
`<Link>` target), and quick links to the three existing admin screens. The summary and
anomalies fetches are independent `.then/.catch` chains, not a single `Promise.all`, so
one endpoint failing only blanks its own section.

Verified live: the dashboard's four numbers matched a direct `psql` cross-check
exactly, and the anomaly panel rendered the live `BulkBeerAdd` anomaly left over from
#58's own smoke test, with a working link to `/admin/beers`.

Suites green: backend 236/236 (229 prior + 7 new), frontend 149/149 (140 prior + 9
new).

- Branch: `feat/59-admin-dashboard` (off `master`)
- PR: [#66](https://github.com/pmconnolly80/FinalCapstone/pull/66) (open, not yet merged)

**This closes Sprint 5: Admin Experience** (issues #53–#59, groomed 2026-07-23, PRs
#60–#66). **Resume here:** groom the Engagement, Retention & Social epic into the next
sprint once #66 merges — per this repo's "only the next epic gets fully broken into
issues" convention, that grooming session hasn't happened yet.

---

## 2026-07-23 — Live testing findings triaged; dev admin + test customer seeded

**Epic:** cross-cutting (not Sprint 5 scope, which is already closed)

The user visited the live site and reported seven issues in one pass. Investigated each
before touching anything, then triaged:

- **Forgot-password emails never send** — confirmed not a code bug: SMTP host/from-address
  are empty in `appsettings.json`, `docker-compose.yml`, and the untracked `.env` alike, so
  `SmtpEmailSender` correctly no-ops per its existing design. Left as-is; tracked under
  Deployment & Hardening in `EPICS_AND_SPRINTS.md`.
- **Nav bar dislike / shouldn't show before login, and the top "tabs" don't make sense** —
  confirmed via reading `App.jsx`: today's nav is a flat, always-visible link list with no
  actual tab component anywhere in the app. Real UX gap, not implemented this session —
  logged as the new **Mobile UI Polish** epic (`EPICS_AND_SPRINTS.md`) alongside the login
  screen ask (logo + minimal fields, pending the bar's eventual color theme).
- **"Beer list doesn't search the API"** — investigated `BeerList.jsx`, `api.js`, and
  `BeersController.GetBeers`; found the existing search correctly wired end to end (debounce,
  param names, endpoint). Asked the user for a repro and learned the actual expectation was
  searching the *external* beer database (Open Brewery DB / Catalog.beer), not the tavern's
  own ~200-beer list — a scope question, not a bug, per `CLAUDE.md`'s product framing. The
  user then asked for this as a real feature, plus letting customers recommend beers for the
  tavern to stock (using search activity as an ordering-decision signal too). Documented as a
  candidate in `FEATURE_MAP.md` and `IMPLEMENTATION_BACKLOG.md` Phase 6 — not yet groomed into
  a story; needs its own scoping session (new entities, admin triage UI, and how the
  external-search UI stays clearly distinct from the tavern's-own-list search).
- **Dev/testing admin + customer accounts** — actually implemented: `SeedData.cs` now seeds
  `admin@tavern.local` / `admin1234` (`Admin` role) and `user1@gmail.com` / `1234User1#!`
  (`Customer` role), following the same bootstrap pattern as the existing dev bartender.
  Two adjustments from what was literally asked, both flagged to the user before coding:
  the app has no username-only login (email+password throughout), so `admin@tavern.local`
  stands in for a literal `admin` username; and the requested `admin` password is only 5
  characters against the app's existing min-8 policy (`Program.cs`), so `admin1234` was used
  instead. This is a dev/testing bootstrap only — doesn't replace the real admin-provisioning
  fix already tracked under Deployment & Hardening. Caught a real test collision along the
  way: `BeersAuthorizationTests` already hardcoded `admin@example.com` for its own ad hoc
  admin fixtures, so the first seed email choice (`admin@example.com`) broke that test by
  colliding on password — moved to `admin@tavern.local` instead.

Added `BeerApi.Tests/Data/SeedDataTests.cs` coverage for both new accounts (role, password,
idempotent re-seed). Full backend suite green: 239/239 (236 prior + 3 new). No frontend
changes this session.

See `EPICS_AND_SPRINTS.md`'s new "Live Testing Findings — 2026-07-23" section for the full
per-item triage and epic placement.

**Resume here:** groom the Engagement, Retention & Social epic (including the new
external-search/recommendations candidate) or scope the new Mobile UI Polish epic into a
sprint — neither has been broken into issues yet.

---

## 2026-07-23 — Product/UX gap analysis; Mobile UI Polish + Beer Discovery groomed into real issues

**Epic:** cross-cutting (Sprint 6/7 grooming, plus `epic:admin`/`epic:retention` extensions)

Continuation of the same day's live-testing session. Asked to go deeper than the seven
surface-level bugs already logged: the user's own framing was that despite five sprints
all marked "done," the app "does not fulfill its goals" for real users yet, and asked for
the customer and admin/owner experience to be thoroughly stress-tested for holes, with
findings turned into decisions rather than silently implemented.

Ran a dedicated gap-analysis pass (general-purpose agent, grounded in `PROJECT_PLAN.md`,
`PERSONAS_AND_USAGE.md`, `FEATURE_MAP.md`, `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1, and the
actual shipped frontend/backend code) and surfaced 12 real gaps — 6 customer-facing, 6
admin/owner-facing — each with 2-4 concrete resolution options. Worked through all 12
with the user via three rounds of decision questions. Full list, decisions, and
reasoning now live in a new living doc, `USABILITY_TESTING.md` (distinct from
`POST_MORTEM.md`, which is a one-time Sprint 1–5 retrospective snapshot, not a doc meant
to be updated as work continues).

Two decisions turned out bigger than their original framing:
- **Bartender onboarding** — the user's answer went past "add an admin invite flow" to
  floating a genuinely different future architecture: bartenders might not need real
  Identity accounts at all, with an admin directly creating a staff record + PIN using
  the bartender's birthday (`MMDDYYYY`, 8 digits) as an easy-to-remember code. Documented
  as an open architecture question in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 rather than
  ticketed — it touches the hardcoded 6-digit PIN assumption and `StaffPin`'s FK to
  `ApplicationUser` throughout the codebase, real design work needed first.
- **Owner vs Admin roles** — `PERSONAS_AND_USAGE.md` had promised these stay separable,
  but the code already merged them. The user's resolution wasn't a permission split;
  it's multiple individually-attributed Admin accounts (mostly already true via
  `AdminAudit`) plus one top-level account that can provision the others — also
  documented, not yet designed/ticketed.
- **Mid-shift availability update** — the user wanted elements of all three proposed
  options combined, which surfaces a real tension: granting bartenders an
  availability-only permission requires them to be authenticated users, directly
  conflicting with the bartender-account-model question above. Flagged as an explicit
  open dependency in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 rather than resolved by
  picking one arbitrarily.

Groomed the concrete, ticket-ready decisions into real GitHub issues (new labels
`epic:ui-polish`, `epic:beer-discovery`, `epic:retention` created first — the last one
existed only in docs before this):
- **Milestone 6, Mobile UI Polish**: [#67](https://github.com/pmconnolly80/FinalCapstone/issues/67)–[#71](https://github.com/pmconnolly80/FinalCapstone/issues/71)
  (bottom tab bar nav hidden pre-login, minimal branded login screen, bartender lockout
  signal, cold-start search hint, graceful offline message)
- **Milestone 7, Beer Discovery & Recommendations**: [#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)–[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73)
  (customer-facing external beer-database search, beer recommendations + admin triage)
- **Ungroomed into a sprint yet** (deliberate deviation from the "only next epic gets
  ticketed" convention, per explicit user direction to narrow priorities across several
  candidate epics at once): [#74](https://github.com/pmconnolly80/FinalCapstone/issues/74)
  (`epic:retention`, rating-prompt + milestone pull-forward), [#75](https://github.com/pmconnolly80/FinalCapstone/issues/75)–[#78](https://github.com/pmconnolly80/FinalCapstone/issues/78)
  (`epic:admin`, reason-field microcopy, user-table filter, bartender invite, dashboard
  reframe)

Also updated `PERSONAS_AND_USAGE.md` (role-separation framing, PIN-lifecycle note,
owner-dashboard framing, three open questions resolved/added) and
`TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 (new "Open architecture questions" subsection) to
carry these decisions forward as living plan content, not just session notes.

Found (not fixed) an unrelated doc/GitHub drift while grooming: Milestone 4 ("Sprint 4:
Auth II") is marked closed in GitHub, but issues #40–#46 are all still open — noted in
`USABILITY_TESTING.md` for a future cleanup pass.

No code changes this half of the session — pure planning/grooming. Backend suite
unaffected (239/239 as of the seed-data changes earlier today).

**Resume here:** pick which of Sprint 6 (Mobile UI Polish) or Sprint 7 (Beer Discovery &
Recommendations) to actually start building, or continue narrowing the two open
architecture questions (bartender account model, multi-admin/owner tiering) before they
block #77/#A2's mid-shift-availability follow-up.

---

## 2026-07-23 — Sprint 8 assigned; both architecture questions decided; Milestone 4 drift fixed

**Epic:** cross-cutting (grooming + tracking cleanup, continuing the same day's usability work)

Picked up exactly where the prior entry's "Resume here" left off, in order:

1. **Fixed the Milestone 4 tracking drift.** Verified all 6 closing PRs (#47–#52) were
   actually merged and each explicitly referenced its issue number in the PR body
   (`gh pr view --json body`), confirming Sprint 4's work is genuinely done — the PRs
   just said "#40:" instead of "Closes #40," so GitHub's auto-close never fired. Closed
   issues #40–#46 with a comment explaining why. Milestone 4 now correctly shows
   0 open / 7 closed.
2. **Created Milestone 8 ("Admin & Engagement UX Follow-ups")** and assigned the 5
   previously-ungroomed issues (#74–#78) to it — bundled by size/independence rather
   than shared epic, matching how Sprint 2's interrupts mixed epics before.
3. **Resolved both open architecture questions from the prior session**, via direct
   questions rather than guessing:
   - **Bartender account model**: keep today's full-`ApplicationUser` model — the
     lighter no-login alternative is rejected. The only real change: PIN length
     becomes configurable (6-8 digits) instead of hardcoded to 6, so a bartender's PIN
     can optionally be birthday-format. New issue [#79](https://github.com/pmconnolly80/FinalCapstone/issues/79).
   - **Mid-shift availability**: layer all three previously-proposed options rather
     than picking one. The user's choice unlocked a synthesis not proposed in the
     prior round: since the bartender is already typing their PIN into the customer's
     phone to confirm a beer — a server-side identity resolution that doesn't require
     a real login session — the same trust mechanism can authorize an availability
     flip in that same moment. This resolves the original tension (a role-based
     permission would have required bartenders to be authenticated users, which
     conflicted with the account-model question) since PIN resolution isn't a login.
     New issues [#80](https://github.com/pmconnolly80/FinalCapstone/issues/80) (PIN-pad
     toggle, primary) and [#81](https://github.com/pmconnolly80/FinalCapstone/issues/81)
     (customer-facing crowd-sourced report, secondary) — house policy (bartender
     texts/calls the admin) is the fallback, no code needed.
   - Both new issues assigned to Milestone 8. Updated #77's body to drop the
     "scope may shift" caveat now that the account-model question is settled.
4. Updated `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 (both questions moved from "open" to
   "decided," with the new issues linked), `PERSONAS_AND_USAGE.md` (PIN-lifecycle note
   and "what Marco does not do" section updated to reflect the PIN-pad availability
   exception), `EPICS_AND_SPRINTS.md` (Sprint 8 section replaces the old
   "ungroomed" list, epics table updated, Milestone 4 fix logged), and
   `USABILITY_TESTING.md` (new "Round 3" section, A1/A2 rows updated from open
   questions to decisions).

No app code changes this session — pure grooming, architecture decisions, and GitHub
tracking cleanup.

**Resume here:** pick which sprint to actually start building — Sprint 6 (Mobile UI
Polish, #67–71), Sprint 7 (Beer Discovery & Recommendations, #72–73), or Sprint 8
(Admin & Engagement UX Follow-ups, #74–81, now including the two architecture
follow-ons). No further open architecture questions blocking any of the three.

---

## 2026-07-23 — Sprint coverage review before build start

**Epic:** cross-cutting (pre-build QA on Sprints 6/7/8)

Before starting any of the three groomed sprints, reviewed each against its own stated
goal and the actual current code (not just the issue text as originally written) to
check whether the sprints, as scoped, would actually close the gaps they were created
for. Grounded the review in real checks rather than assumption — pulled every issue
body via `gh issue view`, and grepped the frontend for hardcoded PIN-length copy and
`App.jsx`'s actual current nav link count rather than guessing.

Found 6 real gaps and closed all of them, with the user's agreement on each:

- **Sprint 6**: #67's new bottom tab bar has no defined home for My PIN, Linked
  Accounts, Privacy Policy, or Sign Out (confirmed `App.jsx` currently has 9 nav
  links plus a footer link — far more than a tab bar holds). New issue
  [#82](https://github.com/pmconnolly80/FinalCapstone/issues/82) (Account/Profile hub
  screen), assigned to Milestone 6.
- **Sprint 7**: #72 would open Catalog.beer (a real, paid, API-keyed dependency) to
  customer traffic with no access-control or rate-limit decision — amended #72 to
  require sign-in plus a rate limit. Also, #72's search-demand logging had nowhere to
  be viewed — new issue [#83](https://github.com/pmconnolly80/FinalCapstone/issues/83)
  (admin external-search demand report), assigned to Milestone 7.
- **Sprint 8**: three issues amended in place — #74 (rating prompt) now requires a
  view/edit affordance on beer detail, since My Beers doesn't exist yet to be the
  promised "editable later" home; #79 (variable-length PINs) now explicitly lists the
  3 files with hardcoded "6-digit" copy found via grep, not just the validation logic;
  #80 (PIN-pad availability flag) now supports toggling back to available (not just
  out-of-stock) and requires a deliberate confirm step, since the original scope would
  have left reactivation stuck needing an admin anyway and risked an accidental flip
  next to a routine action.

Updated `EPICS_AND_SPRINTS.md`'s Sprint 6/7/8 issue lists and `USABILITY_TESTING.md`
(new "Round 4" section) to match the amended GitHub issues.

No app code changes this session — pure coverage review and issue refinement.

**Resume here:** all three sprints (6, 7, 8) are now reviewed for coverage and ready to
build, 8/2/8 issues respectively (10 total added/amended this round: #82, #83 new;
#72, #74, #79, #80 amended). Pick one to start.

---

## 2026-07-23 — Sprint 6: Mobile UI Polish

**Epic:** cross-cutting UX (`beer-app/frontend` only, no backend changes)

Built all six groomed Sprint 6 issues in one pass, since the tab bar (#67) and the
Account hub (#82) are two halves of the same change and the rest are small,
independent UI fixes:

- #67: `App.jsx`'s flat top nav replaced with a fixed bottom tab bar
  (Home/Beers/My Progress/Account), rendered only when `auth.signedIn` — nothing
  role-specific lives directly in the tab bar itself.
- #82: new `Account.jsx` hub at `/account` — My PIN (staff only), Linked Accounts,
  Privacy Policy, Sign Out for everyone; Dashboard/Confirmations/Users/Manage Beers
  added underneath for Admin roles. This is where Sign Out moved to (it used to be a
  nav button); `App.jsx` no longer needs `useNavigate`/`handleSignOut` at all.
- #68: `AuthPage.jsx` converted from inline styles to Tailwind, centered card layout,
  placeholder wordmark/emoji logo slot above the form — no color theme decided yet,
  matches the issue's explicit scope.
- #69/#71: `confirmBeer` in `api.js` now catches a `fetch` throw (no network path at
  all) and rethrows with an `isNetworkError` flag, distinguishing it from a normal
  non-ok response; `ConfirmPinPad.jsx` shows a distinct "No signal — ask the
  bartender..." message for that case, and after 3 consecutive non-network failures
  shows an "ask an admin" cue — both purely client-side, no change to the generic 401
  the API already returns.
- #70: `BeerList.jsx` now calls `fetchMyProgress` for signed-in customers and shows a
  first-visit hint ("try filtering by style...") when `confirmedCount === 0`,
  alongside the existing had/not-had chips.

Updated `App.test.jsx` (nav-hidden-when-signed-out, tab bar contents) and added
`Account.test.jsx`; extended `BeerList.test.jsx` and `ConfirmPinPad.test.jsx` for the
new behavior. Frontend suite: 158/158. Backend suite (unaffected, re-run for
confidence): 239/239. Clean `npm run build`.

No browser automation was pre-configured in this environment, so before merging, set
one up ad hoc via `npx playwright install chromium` and wrote a driving script
(iPhone-sized viewport) exercising: nav absence when signed out, sign-out/sign-in
round trip, the bottom tab bar and Account hub for both a fresh customer and the
seeded admin account (`admin@tavern.local`), the BeerList first-visit hint, and both
the offline (`page.context().setOffline(true)`) and repeated-PIN-failure messages on
the confirmation PIN pad — 16/16 assertions passed, with screenshots reviewed
visually too, not just DOM checks.

- Branch: `sprint-6-mobile-ui-polish` — [PR #84](https://github.com/pmconnolly80/FinalCapstone/pull/84)
  merged to `master`, closed #67, #68, #69, #70, #82 automatically; #71 didn't
  auto-close from the merge commit's closing keywords and was closed manually,
  cross-referencing PR #84. Milestone [#6](https://github.com/pmconnolly80/FinalCapstone/milestone/6)
  closed.

**Resume here:** Sprint 6 is done. Sprint 7 (Beer Discovery & Recommendations,
#72/#73/#83) and Sprint 8 (Admin & Engagement UX Follow-ups, #74–#81) are both still
groomed and ready to build next.

---

## 2026-07-23 — Sprint 7: Beer Discovery & Recommendations

**Epic:** `epic:beer-discovery`

Built all three groomed Sprint 7 issues in one pass, in dependency order (#72 → #73 →
#83, matching how each depends on the one before it):

- #72: new `BeerLookupController` (`GET /api/beer-lookup/search`) reuses the existing
  `ICatalogBeerService`/`IBreweryLookupService` (previously Admin-only via
  `CatalogBeerController`/`BreweriesController`) behind a signed-in-only endpoint,
  returning `{ beers, breweries }`. Rate-limited to 20 requests/minute per user —
  ASP.NET Core's built-in `RateLimiter` was net-new to this codebase, wired up in
  `Program.cs` as a `"PerUserExternalSearch"` fixed-window policy partitioned by
  `ClaimTypes.NameIdentifier`, with `RejectionStatusCode` explicitly set to 429 (the
  default is 503). Every call logs to a new `ExternalSearchLog` table, computing
  `MatchedTavernCatalog` by reusing `BeersController`'s own substring-match logic.
  `BeerList.jsx` gained a "What's on our list" / "Look up any beer" mode toggle
  (signed-in only), with lookup results in visually distinct amber-bordered cards and
  a "Recommend this beer" button per hit.
- #73: new `BeerRecommendation` entity (`Status` enum New/Reviewed/Added/Declined,
  text-converted like `BeerAvailability`) plus `RecommendationsController` (customer
  submission, only `BeerName` required) and `AdminRecommendationsController`
  (filterable list + `PATCH .../status`, no reason required — closer to the
  availability PATCH's immediate toggle than confirmation-void's reason guard). New
  `RecommendBeer.jsx` (`/recommend`, prefillable from a lookup-mode search hit via
  `location.state`) and `AdminRecommendations.jsx` (`/admin/recommendations`).
- #83: new `AdminExternalSearchController` (`GET /api/admin/external-search-demand`)
  aggregates unmatched `ExternalSearchLog` rows by frequency via a `public static
  ComputeDemandAsync(context, now, sinceDays, topN)` — same explicit-`now`-parameter
  testability pattern as `AdminAnomaliesController`/`AdminDashboardController`. New
  `AdminSearchDemand.jsx` (`/admin/search-demand`) renders the table with its own
  independent fetch/error state, matching `AdminDashboard.jsx`'s anomalies panel.

`Account.jsx` gained "Recommend a beer" (all signed-in users) and
"Recommendations"/"Search Demand" (Admin-only) links.

Suites: backend 271/271 (+32 new: `BeerLookupControllerTests`,
`RecommendationsControllerTests`, `AdminRecommendationsControllerTests`,
`AdminExternalSearchControllerTests`, plus HTTP-level auth/rate-limit integration
tests for each new endpoint). Frontend 175/175 (+17 new: `BeerList.test.jsx`'s new
"#72 external lookup mode" describe block, `RecommendBeer.test.jsx`,
`AdminRecommendations.test.jsx`, `AdminSearchDemand.test.jsx`, extended
`Account.test.jsx`). Clean `npm run build`.

Verified live against the Docker stack (no browser automation available, so via curl
per the `verify` skill): real Catalog.beer/Open Brewery DB results for "duvel", a
plain-text and a search-hit recommendation both submitted and triaged to `Added` by
the seeded admin (`admin@tavern.local`) with the change persisting across a re-fetch,
the demand report showing one unmatched query after a gibberish search, the tavern's
own catalog search (`GET /api/beers?search=`) unaffected, anonymous requests to
`/api/beer-lookup/search` rejected with 401, and the rate limit correctly tripping to
429 on the 20th+ request from one user within a minute.

- Branch: `sprint-7-beer-discovery-recommendations` —
  [PR #85](https://github.com/pmconnolly80/FinalCapstone/pull/85), merged to `master`.

## 2026-07-23 — Sprint 8 planning: PR #85 merged, build order set, #77 built

**Epic:** `epic:admin`

Planning session for Sprint 8 (Admin & Engagement UX Follow-ups, #74–#81, already
groomed). First merged PR #85 (Sprint 7) to `master` (CI green) so Sprint 8 branches
off a clean base. Then mapped file overlap across the 8 issues to set a one-PR-per-issue
build order rather than the single-combined-PR approach Sprint 7 used (per this
session's explicit direction): `AdminUsers.jsx` is touched by #75/#76/#79/#77 and
`ConfirmPinPad.jsx`/`BeerDetail.jsx` by #79/#80/#74/#81, so those two chains build
sequentially (#75 → #76 → #79 → #77, then #80 → #74 → #81) with #78 (fully
independent, `AdminDashboard.jsx` only) free to slot anywhere.

Built #77 (admin-initiated bartender invite) first, out of that proposed order (per
explicit direction), since nothing else had touched `AdminUsers.jsx` yet on a clean
`master` — no conflict risk from skipping ahead:

- New `POST /api/admin/users/invite-bartender` on `AdminUsersController` (`Admin`-only,
  no reason required — an account-creation action, not a correction to an existing one,
  so it doesn't fit the reason-guard pattern #53–#56 use). Creates the `ApplicationUser`
  directly in the `Bartender` role, then reuses `AuthController`'s existing
  `GeneratePasswordResetTokenAsync`/`reset-password` flow as the "set your password"
  link — `ResetPasswordAsync` works identically whether or not a password was ever set,
  so no new frontend page was needed; `ResetPassword.jsx` already does this. Writes an
  `AdminAudit` row (`Action = "Invite"`, no reason, same as `PostBeer`'s `"Create"` audit
  from #58). Existing-email invites 409 rather than creating a duplicate account.
- `AdminUsers.jsx` gained a direct (un-guarded, no reason step) "Invite a new bartender"
  email form above the table, wired through a new `inviteBartender()` in `api.js`.

Suites: backend 280/280 (+9 new: unit tests on `AdminUsersControllerTests` covering
missing-email/existing-email/happy-path+audit, and integration tests on
`IntegrationTests/AdminUsersTests.cs` covering auth gating and a full invite →
extract-token-from-email → `reset-password` → `login` round trip). Frontend 177/177
(+2 new on `AdminUsers.test.jsx`). Clean `npm run build`.

Verified live against the Docker stack via curl (per the `verify` skill, no browser
automation available): invited `livetest.bartender@example.com` as the seeded admin
(`admin@tavern.local`), confirmed the account landed in the `Bartender` role and the
`AdminAudit` row was written via direct `psql`, unauthenticated invite rejected with
401, and a duplicate invite rejected with 409. SMTP isn't configured in this
environment (a known, existing gap — see `EPICS_AND_SPRINTS.md`'s deployment-epic
note), so the invite email itself silently no-ops rather than sending, same as
`forgot-password` already does.

- Branch: `sprint-8-bartender-invite` —
  [PR #86](https://github.com/pmconnolly80/FinalCapstone/pull/86), CI green, merged to
  `master` before starting #75 (keeps the `AdminUsers.jsx` chain conflict-free).

Then built #75 (staff-only filter + search on the User Management table), next in the
build order: `AdminUsers.jsx` now defaults to showing only Bartender/Admin rows (a
"Show all users (including customers)" checkbox reveals the rest), plus a client-side
email filter box mirroring `AdminConfirmations.jsx`'s existing filter pattern.
Frontend-only — the API already returns every user, this is purely a view filter, so
no backend change or new backend tests. The acceptance criteria said "email/name
search," but the user model has no separate display name field, so the filter
searches email only (a deliberate scope call, not a gap).

Existing `AdminUsers.test.jsx` tests that assumed a customer row was visible by
default needed updating for the new default (either switched to a staff row, or
added a "show all" toggle click first) — this was expected given the acceptance
criteria, not a regression. Frontend suite: 180/180 (+3 new: staff-only default
view, show-all toggle round-trip, email filter). Clean `npm run build`.

Verified live: rebuilt the `web` container (`docker compose up -d --build web`),
confirmed the Vite dev server serves the new "Show all users"/"Filter by email" UI
text via `curl http://localhost:3001/src/pages/AdminUsers.jsx`, and confirmed
`GET /api/admin/users` (unchanged) still returns the same response shape via curl as
the seeded admin.

- Branch: `sprint-8-user-mgmt-filter` —
  [PR #87](https://github.com/pmconnolly80/FinalCapstone/pull/87), CI green, merged to
  `master` before starting #76 (keeps the `AdminUsers.jsx` chain conflict-free).

Then built #76 (inline consequence microcopy on audited admin actions), next in the
build order. Copy-only, no backend change, per the issue's own acceptance criteria:
short lines of microcopy shown right at the point of each audited action, only once
it's pending (not on page load) —

- `AdminUsers.jsx`: a new `CONSEQUENCE_MICROCOPY` map for the role-change/deactivate/
  reactivate guard steps. The deactivate/reactivate copy comes straight from
  `AdminUsersController`'s existing behavior (documented in
  `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1, just never surfaced in the UI before now).
  The role-change copy is a genuinely new finding from reading
  `ConfirmationsController.ResolveBartenderFromPinAsync` while writing this story: it
  only resolves PINs belonging to a *current* Bartender/Admin, so moving someone off
  those roles silently stops their PIN from working for confirmations, even though
  `StaffPin.IsActive` stays `true` — nothing had ever said this in the UI.
- `AdminConfirmations.jsx`: microcopy at the void step itself (not just the existing
  page-level paragraph) states the mug-not-revoked behavior from §4.1.
- `AdminBeers.jsx`: microcopy at the delete step, plus a general note above the table.
  Researching this surfaced a second real, previously-undocumented gap:
  `BeerConfirmation.BeerId` is a restrict-on-delete FK
  (`ApplicationDbContext.OnModelCreating`), so `DeleteBeer` on a beer with existing
  confirmations doesn't cascade — it throws an unhandled `DbUpdateException`
  server-side, which the frontend's generic error handling only ever shows as "Failed
  to delete beer," no real explanation. The microcopy warns admins away from hitting
  this rather than fixing the exception handling itself, since that's backend
  behavior change out of this copy-only issue's scope — flagged in
  `EPICS_AND_SPRINTS.md`/`CLAUDE.md` as a worthwhile small follow-up story, not
  silently fixed here.

Suites: frontend 183/183 (+6 new: one microcopy-visibility test per action across
`AdminUsers.test.jsx`, `AdminConfirmations.test.jsx`, `AdminBeers.test.jsx`, checking
each string is absent until the action is pending). Clean `npm run build`.

Verified live: rebuilt the `web` container, confirmed all five new microcopy strings
are served by the Vite dev server across the three page source files via curl.

- Branch: `sprint-8-admin-microcopy` —
  [PR #88](https://github.com/pmconnolly80/FinalCapstone/pull/88), CI green, merged to
  `master`.

## 2026-07-24 — Bug fix: DeleteBeer's unhandled 500 on a confirmed beer (out-of-milestone, follows from #76)

**Epic:** none — a standalone bug fix, not a Sprint 8 issue, done per explicit user
direction to fix the real gap #76 had flagged rather than let it sit for a future
session.

Confirmed the bug live against the Docker stack before fixing it: `DELETE
/api/beers/2` (a seeded beer with 3 existing `BeerConfirmation` rows) returned a bare
500 with a raw Npgsql/EF `DbUpdateException` stack trace in the response body — real
in Development mode, and an unhandled exception either way. Root cause:
`BeerConfirmation.BeerId` is a restrict-on-delete FK
(`ApplicationDbContext.OnModelCreating`), so Postgres itself rejects the delete;
`BeersController.DeleteBeer` never checked for this before calling
`SaveChangesAsync`. This bug was invisible to the entire existing test suite, since
every backend test (unit and the `WebApplicationFactory`-based integration tests)
runs against EF Core's InMemory provider, which doesn't enforce relational FK
constraints the way Postgres does — only live-stack testing against real Postgres
could have surfaced it, which is exactly how it was originally found while writing
#76's `AdminBeers.jsx` delete-step microcopy.

Fix: `DeleteBeer` now checks `_context.BeerConfirmations.AnyAsync(c => c.BeerId ==
id)` up front and returns a clean `409 Conflict` with the same guidance #76's
microcopy already gives customers-facing admins ("Void those confirmations first, or
mark it Retired instead") — an explicit check rather than catching the DB exception,
which also makes it deterministically testable against the InMemory provider instead
of depending on provider-specific exception behavior. `AdminBeers.jsx` needed no code
change at all: its existing generic `error.message` handling in the catch block
already surfaces whatever message the API sends.

Suites: backend 282/282 (+2 new — a controller unit test seeding a
`BeerConfirmation` directly to exercise the InMemory path, and an HTTP-level
integration test confirming the 409 status and message). Frontend 184/184 (+1 new,
asserting the specific conflict message renders in `AdminBeers.jsx`). Clean
`npm run build`.

Verified live against the Docker stack, both directions: `DELETE /api/beers/2`
(existing confirmations) now returns the clean 409 message instead of a stack trace;
creating a fresh beer with no confirmations and deleting it still returns 204 as
before — confirming the fix doesn't over-block legitimate deletes.

- Branch: `fix-delete-beer-with-confirmations` —
  [PR #89](https://github.com/pmconnolly80/FinalCapstone/pull/89), CI green, merged to
  `master`.

## 2026-07-24 — Sprint 8 #79: variable-length staff PINs (planned out before building)

**Epic:** `epic:admin`

Planned the approach with the user before building, per explicit request. PINs were
hardcoded to exactly 6 digits in 5 places: `StaffPinsController.SetPinAsync`,
`ConfirmationsController.PostConfirmation` (both backend length checks), plus 3
frontend files' validation/`maxLength`/copy — `ConfirmPinPad.jsx`, `MyPin.jsx`, and
`AdminUsers.jsx`'s Set PIN step (exactly the 3 files the issue's acceptance criteria
called out by name). Relaxed to a configurable 6-8 digit range so an admin can issue
a bartender a longer, memorable format (an 8-digit birthday, `MMDDYYYY`) instead of a
random 6-digit one:

- New `StaffPinsController.MinPinLength`/`MaxPinLength` constants replace both
  backend hardcoded checks — same cross-controller reuse pattern already established
  by `MeController` referencing `ConfirmationsController.MugGoal`, so the two entry
  points (setting a PIN, confirming with one) can't drift apart.
- Frontend: `ConfirmPinPad.jsx`'s input cap raised from 6 to 8 and its error message
  made length-agnostic ("Enter the bartender's PIN." rather than claiming "6-digit");
  `MyPin.jsx` and `AdminUsers.jsx`'s Set PIN step both got their regex/`maxLength`/
  placeholder/error copy updated to state the real 6-8 range rather than a fixed
  "6-digit" claim — the user's stated preference during planning was for copy that
  states the actual range rather than fully vague wording, since it's more useful to
  someone picking a PIN.
- Existing 6-digit PINs (the dev bartender's `123456`) keep working unchanged — the
  range check is inclusive of 6, not a replacement floor.

Two existing backend unit tests needed fixing, not just extending: both
`StaffPinsControllerTests.SetMyPin_MalformedPin_ReturnsBadRequest` and
`ConfirmationsControllerTests.PostConfirmation_MalformedPin_ReturnsBadRequest` had a
7-digit PIN (`"1234567"`) in their "malformed" `[InlineData]` sets — correct under the
old hardcoded-6 rule, but now genuinely valid input, so those cases were swapped for
actually-out-of-range ones (9-digit / 5-digit).

Per the issue's explicit acceptance criteria ("regression tests cover both a 6-digit
and an 8-digit PIN through the full confirm + lockout flow"), extended
`ConfirmationsFlowTests.cs` with an HTTP-level test seeding a fresh bartender with an
8-digit PIN directly via the DbContext, then running the same confirm-success +
5-failed-attempts-then-locked sequence every other test in that file already runs
against the seeded dev bartender's 6-digit PIN. Hit one test bug along the way: the
first draft reused the same beer for both the success-path confirm and the lockout
loop, and `PostConfirmation`'s already-confirmed check runs *before* PIN validation —
so every lockout-loop attempt 409'd immediately regardless of PIN, never actually
exercising the lockout path. Fixed by using two separate beers.

Suites: backend 287/287 (+5 new — 2 unit tests plus the HTTP-level 8-digit
confirm+lockout regression test), frontend 187/187 (+3 new: an 8-digit success case
each for `ConfirmPinPad.jsx`, `MyPin.jsx`, and `AdminUsers.jsx`'s Set PIN step).
Clean `npm run build`.

Verified live against the Docker stack: invited a fresh bartender via #77's endpoint,
issued them an 8-digit birthday-format PIN (`07041999`) via the admin API, confirmed
a beer with it as a customer (201), confirmed the existing dev bartender's 6-digit
PIN still works unchanged (201), confirmed an unissued-but-in-range 7-digit PIN is
rejected as "Invalid PIN" (401, the wrong-PIN path, not a validation error — proving
the range check isn't accidentally accepting any 6-8 digit string as a match), and a
too-short 5-digit PIN is rejected at validation (400, "A PIN of 6-8 digits is
required."). Also confirmed all the new frontend copy ("PINs must be 6-8 digits",
"PIN (6-8 digits)", "Enter the bartender's PIN") is served live by the Vite dev
server across the three page/component source files.

- Branch: `sprint-8-variable-pin-length` —
  [PR #90](https://github.com/pmconnolly80/FinalCapstone/pull/90), CI green, merged to
  `master`.

## 2026-07-24 — Sprint 8 #80: mark a beer out-of-stock from the confirmation PIN pad

**Epic:** `epic:admin`

Built #80 next in the build order (depends on #79's relaxed PIN validation, reuses
`ConfirmPinPad.jsx`), diving straight into implementation this time rather than a
separate planning pass first. Per `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1's decision:
a bartender has no device or login session of their own at the bar (the one-device
rule), so this piggybacks on the exact same PIN-typed-into-the-customer's-phone
trust moment `POST /api/confirmations` already uses, instead of requiring a separate
Admin-gated session.

- New `POST /api/confirmations/availability` on `ConfirmationsController`
  (`PinAvailabilityRequest{BeerId,Pin,Availability}`) — deliberately narrower than
  `BeersController.UpdateAvailability` (Admin-only, all 4 states): only
  `OutOfStock`/`Available` are accepted here, the two a bartender plausibly needs
  mid-shift; `OnTap`/`Retired` attempts 400. Every change is audited attributed to
  the *resolved bartender's* id, not "Admin" — same `AdminAudit` table/shape #56
  already established, just a different attribution source.
- Refactored `PostConfirmation`'s inline PIN-resolution/lockout/customer-brute-force-
  window logic into a shared `AuthorizeBartenderPinAsync` helper so the new endpoint
  reuses it verbatim, rather than duplicating it. Both endpoints now share one
  `FailedConfirmationAttempt` counter per customer — spreading guesses across
  confirming a beer and flipping availability doesn't grant a bigger PIN-guessing
  budget than using either endpoint alone would. Also extracted the PIN
  format-validation check (`request.Pin.Length` etc.) into a small `IsValidPin`
  static helper for the same reason, since both endpoints needed the identical check.
- `ConfirmPinPad.jsx` gained a "Mark this beer as out of stock" / "...as available"
  link below the main confirm form — the label flips based on the beer's current
  `availability` prop, so it always proposes the sensible direction. It reuses
  whatever PIN is already typed into the field above (a "closely-related follow-up
  call using the same PIN," per the issue's acceptance criteria) rather than asking
  for it a second time. Clicking it requires a deliberate second tap ("Yes, mark out
  of stock") before anything submits, and shows a distinct "✅ Marked out of stock."
  success message — this sits right next to the routine, fast Confirm action, so a
  single accidental tap mid-rush can never silently change anything. New
  `setBeerAvailabilityViaPin()` in `api.js`, same network-error distinction
  `confirmBeer` already has.

Suites: backend 299/299 (+12 new — unit tests for both directions, the no-op case,
both disallowed target states, malformed/wrong PIN, and unknown beer; two HTTP-level
integration tests: the happy path both directions with audit-attribution assertions,
and wrong-PIN rejection byte-for-byte matching a bad confirmation's rejection body).
Frontend 193/193 (+6 new). Clean `npm run build`.

Verified live against the Docker stack: marked a real beer out of stock via the dev
bartender's PIN (204), flipped it back to available (204), confirmed both audit rows
via `psql` are attributed to `bartender@example.com`'s user id rather than an admin
account, confirmed requesting the disallowed `OnTap` target 400s
("Availability must be OutOfStock or Available."), and confirmed a wrong PIN gets
the byte-identical `{"message":"Invalid PIN."}` 401 body a bad confirmation attempt
gets. Also confirmed the new "Mark this beer as..."/"Yes, mark..." UI text is served
live by the Vite dev server.

- Branch: `sprint-8-pin-availability-flip` —
  [PR #91](https://github.com/pmconnolly80/FinalCapstone/pull/91), CI green, merged
  to `master`.

## 2026-07-24 — Sprint 8 #74: "How was it?" rating prompt + minimal milestone moment

**Epic:** `epic:retention`

Built #74 next in the build order, per the "continue as planned" instruction — no
separate up-front planning pass this time, straight to implementation (per the
user's own steer earlier this session that dive-straight-in is fine once the
pattern's established). Pulls two cheap, high-signal pieces of the full Engagement
epic (`IMPLEMENTATION_BACKLOG.md` Phase 6 — My Beers ratings, milestone badges)
forward, since the confirmation loop otherwise just ends at a bare X-of-200 number.

- New `BeerRating` entity (`CustomerId`/`BeerId` unique, restrict-on-delete FK to
  `Beer` — same pattern as `BeerConfirmation`) + migration.
  `PUT /api/me/ratings/{beerId}` on `MeController` upserts in place (create-or-update
  in one call, `StaffPinsController.SetPinAsync`-style rather than separate
  create/update endpoints), validates 1-5, and requires an existing confirmation for
  the same beer (400 otherwise — "You can only rate a beer you've confirmed.").
- `BeersController.GetBeer` now also returns `Confirmed`/`MyRating`, computed the
  same per-customer-claim way `GetBeers`' search endpoint already computes its
  per-item `Confirmed` flag. Since My Beers doesn't exist yet, beer detail ends up
  being the only place a rating can be viewed or corrected after the PIN pad moment
  passes — exactly the gap the issue's acceptance criteria was amended to cover.
- Milestone: a new `ConfirmationsController.MilestoneCount = 100` constant and a
  `MilestoneReached` flag on `ConfirmationResponse`, computed transiently
  (`confirmedCount == MilestoneCount`) — deliberately not a durable award table like
  `MugAward`, since the issue explicitly scoped this as "not full badge
  infrastructure." 100 lines up with `IMPLEMENTATION_BACKLOG.md`'s own
  "milestone badges at 25/50/100/150" list.
- `ConfirmPinPad.jsx`'s success screen gained a 1-5 star "How was it?" prompt
  (skippable; re-tapping a different star re-submits, since "editable later" also
  means editable right there) and a milestone banner (🎉) shown only when the mug
  wasn't also earned on that same confirmation, so the two celebratory moments never
  compete for the same screen. `BeerDetail.jsx` gained a "Your rating" section,
  visible only for beers the customer has actually confirmed, with the existing
  rating highlighted and any star re-clickable to change it. New `setMyRating()` in
  `api.js`.

Suites: backend 312/312 (+13 new — rating create/update-in-place/out-of-range/
no-confirmation-yet on `MeControllerTests`, `GetBeer`'s new fields including that
ratings stay private per customer on `BeersControllerTests`, the milestone flag at
exactly 100 and one below it on `ConfirmationsControllerTests`, plus two HTTP-level
integration tests — the full rating lifecycle end to end, and driving a real
customer through 100 confirmations to check the milestone fires exactly once).
Frontend 201/201 (+8 new across `ConfirmPinPad.test.jsx`/`BeerDetail.test.jsx`).
Clean `npm run build`.

Verified live against the Docker stack: rated a real confirmed beer, edited that
rating and confirmed the change persisted, confirmed a second customer sees no
rating for the same beer and `confirmed: false` for themselves (ratings and
confirmed-status are both per-customer, never leak across accounts), rejected an
out-of-range rating (400) and rejected rating an unconfirmed beer (400), and drove a
real customer through exactly 100 confirmations via a loop of admin-created
beers — the 100th response came back with `milestoneReached: true` and
`mugEarned: false`, confirming the two moments are independent.

- Branch: `sprint-8-rating-milestone` —
  [PR #92](https://github.com/pmconnolly80/FinalCapstone/pull/92), CI green, merged
  to `master`.

## 2026-07-24 — Sprint 8 #81: customer-facing "flag beer as unavailable" report

**Epic:** `epic:admin`

Built #81, the second and independent layer of the mid-shift-availability decision
alongside #80's bartender PIN-pad toggle — a crowd-sourced signal from customers,
not a direct availability change (that would be a griefing vector: anyone could mark
any beer unavailable with no verification).

- New `UnavailabilityReport` entity (restrict-on-delete FK to `Beer`, same pattern as
  `BeerConfirmation`/`BeerRating`) + migration.
  `POST /api/beers/{id}/unavailability-reports` on `BeersController` — `[Authorize]`,
  any signed-in role, deliberately not gated to confirmed-only, since an uncertain
  customer walking up to an empty tap is exactly the person this is for. A repeat
  report from the same customer for the same beer within a 24h window is a silent
  no-op (no duplicate row, no error) — the window matches the anomaly signal's own
  lookback exactly (`BeersController.UnavailabilityReportWindowHours`), so the
  dedup window and the counting window can't drift apart and a customer can't
  inflate the count by tapping repeatedly.
- Rather than a new admin screen, this extends the existing #58 anomaly panel — "an
  alert similar in spirit to the existing anomaly panel" per the issue's own
  framing. New 4th signal `AdminAnomaliesController.DetectUnavailabilityReportsAsync`:
  one entry per beer with at least one recent report, grouped and counted over a
  configurable lookback (`Anomalies:UnavailabilityReports:LookbackHours`, default
  24, same `IConfiguration`-direct-read pattern as the other three signals).
  "More prominent than a single one" is expressed directly in the summary text's
  count ("flagged unavailable by 3 customers" reads as more urgent than "by 1") and
  via `OccurredAt` being the most-recently-reported time — a beer getting
  repeatedly flagged naturally sorts near the top of the combined, all-signal-types
  anomalies list — rather than a separate severity tier, matching the issue's own
  "no need for anything fancier."
- `BeerDetail.jsx` gained a "Report this as unavailable" link, reachable by any
  signed-in customer regardless of whether they've confirmed the beer, with a
  thank-you message on success. `AdminDashboard.jsx`'s existing `ANOMALY_BADGES` map
  gained an entry for the new type. New `reportBeerUnavailable()` in `api.js`.

Suites: backend 323/323 (+11 new — report creation, same-customer dedup within the
window, two distinct customers both counting, unknown beer 404 on
`BeersControllerTests`; the anomaly signal's count-in-summary wording (plural vs.
singular), outside-the-lookback-window exclusion, and per-beer grouping on
`AdminAnomaliesControllerTests`; plus an HTTP-level integration test proving a real
customer's report actually surfaces through the real `/api/admin/anomalies`
endpoint to a real admin token). Frontend 206/206 (+5 new across
`BeerDetail.test.jsx`/`AdminDashboard.test.jsx`). Clean `npm run build`.

Verified live against the Docker stack: reported a real beer ("60 Minute IPA") as
unavailable, submitted a second report from the same customer for the same beer and
confirmed via `psql` that only one row exists (dedup held), confirmed the report
shows up in `GET /api/admin/anomalies` with the correct beer name, count, and a
`/beers/{id}` deep link, confirmed an unauthenticated report attempt 401s, and
confirmed reporting an unknown beer id 404s.

- Branch: `sprint-8-unavailability-reports` —
  [PR #93](https://github.com/pmconnolly80/FinalCapstone/pull/93), CI green, merged
  to `master`.

## 2026-07-24 — Sprint 8 #78: reframe Admin Dashboard as operational health — closes Sprint 8

**Epic:** `epic:admin`

Built the last remaining Sprint 8 issue, fully independent of the two file-overlap
chains (`AdminDashboard.jsx` only). The shipped Admin Dashboard (#59) surfaced
operational counts and the anomaly panel, but was implicitly expected to also
answer the owner's real question — "what should I order more of / who's about to
lapse" (`PERSONAS_AND_USAGE.md`'s "Weekly ritual") — which it never actually did.

- `AdminDashboard.jsx`'s summary-cards section is now explicitly labeled
  "Operational health," with copy stating up front that beer-purchasing
  intelligence (demand, ratings, lapsed members) lives in a separate, later Owner
  Analytics screen rather than being implied as already covered here.
- New `AdminDashboardController.ComputeBeerConfirmationCountsAsync` (`TopN = 5`,
  same public-static-with-no-time-dependency pattern as `ComputeSummaryAsync`) — a
  plain `GROUP BY` over existing `BeerConfirmation` rows, no new schema, exposed as
  `GET /api/admin/dashboard/beer-confirmations`. Deliberately counts every beer, not
  just ones with at least one confirmation row — a plain `GroupBy` over
  `BeerConfirmations` alone would silently omit a beer with zero confirmations
  entirely, which is exactly "the stout nobody's ordered in two months" this feature
  exists to surface, so it left-joins against the full `Beers` table instead.
- New "Most / least confirmed beers" panel on the dashboard (top 5 each, linking to
  each beer's detail page), fetched independently of the summary/anomalies calls (a
  broken endpoint only blanks its own section, matching the existing pattern). New
  `fetchBeerConfirmationCounts()` in `api.js`.
- Full "beer intelligence" (want-list demand counts, anonymized ratings,
  lapsed-member list) stays explicitly out of scope — deferred to Owner Analytics
  once the Engagement/Retention epic gets its own grooming session.

Suites: backend 328/328 (+5 new — ranking most/least by count, a genuinely
zero-confirmation beer still appearing in "least confirmed" rather than being
silently dropped, the top-N limit, plus an HTTP-level integration test). Frontend
209/209 (+3 new — the "Operational health" reframe copy, the new panel rendering
with working deep links, and its own independent error state). Clean
`npm run build`.

Verified live against the Docker stack: `GET /api/admin/dashboard/beer-confirmations`
returned real ranked data cross-checked against beers created earlier in this
session — the genuinely busiest beer at the top of "most confirmed," and several
beers with zero confirmations correctly appearing in "least confirmed" rather than
being omitted. Confirmed the new "Operational health" and "Most / least confirmed
beers" UI text is served live by the Vite dev server.

- Branch: `sprint-8-dashboard-reframe`, not yet opened as a PR.

**Resume here:** open the PR for #78. Once merged, **Sprint 8 (Admin & Engagement
UX Follow-ups) is fully closed** — all of #74–#81 built and merged. Next: the
**Engagement, Retention & Social** epic (milestone badges, push notifications +
owner composer, My Beers, social feed, journal, owner analytics) is the next
candidate for grooming into its own sprint — that grooming session hasn't happened
yet. Deployment & Hardening follows after that.
