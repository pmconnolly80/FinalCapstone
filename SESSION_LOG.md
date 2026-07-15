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
