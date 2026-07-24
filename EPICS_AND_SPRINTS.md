# Epics & Sprints

This is the project's Agile tracking doc — the markdown mirror of what's tracked live in
GitHub. This project runs as a solo, session-based effort rather than a team on a fixed
calendar, so it uses a **scope-based** sprint model instead of time-boxed weeks: a sprint is a
fixed bundle of stories and it closes when that scope is done, however many sessions that takes.

## How this maps to GitHub

No separate PM tool — GitHub's own primitives cover it:

- **Milestone = Sprint.** A milestone bundles the stories for one sprint, tracks percent-closed
  automatically, and closes when done (no due date required).
- **Label = Epic.** Epics span more than one sprint, so they're a cross-cutting label
  (`epic:mug-club`, `epic:auth`, etc.) rather than a milestone.
- **Issue = Story/Task**, labeled with its epic and assigned to the sprint (milestone) it's
  scheduled in.

Browse live: [Issues](https://github.com/pmconnolly80/FinalCapstone/issues) ·
[Milestones](https://github.com/pmconnolly80/FinalCapstone/milestones) ·
[Labels](https://github.com/pmconnolly80/FinalCapstone/labels)

Only the **next** epic gets fully broken into GitHub issues at any given time — later epics are
named below so the roadmap is visible, but aren't ticketed until they're actually up next
(real backlog grooming, not pre-ticketing work that's sprints away and likely to change shape).

## Definition of Done

This is a TDD project: starting now, every story ships with tests — unit and/or integration —
before it's considered done, not added after the fact. `.github/workflows/tests.yml` runs the
backend (`dotnet test`) and frontend (`npm test`) suites on every push/PR to `master`, so this
is enforced, not just stated. See `CLAUDE.md`'s "Testing policy (TDD)" section for where tests
live and how to run them locally.

## Branching

One feature branch per PR, opened off `master`, merged before the next branch starts —
sequential, not stacked. Stacking a new branch on top of an unmerged one means dragging
its rebase/conflict risk into the next piece of work for no benefit in a solo, session-based
project with no real review-wait latency. (Convention made explicit 2026-07-22 for #40/#41,
[PR #47](https://github.com/pmconnolly80/FinalCapstone/pull/47) — every merged PR from #7
onward already followed this in practice, it just hadn't been written down.)

## Bugs

Defects against already-shipped work are filed as issues with the `bug` label plus the owning
`epic:*` label. A bug that blocks the current sprint's work (or blocks basic use of the
product) gets pulled into the open sprint milestone as an **interrupt**; otherwise it waits
for grooming like any other story. Bug fixes follow the same TDD Definition of Done — the fix
ships with a test that would have caught it. (Convention established 2026-07-14 when live
testing found the registration flow broken, see #17.)

## Epics

| Epic | Label | Status |
|---|---|---|
| Core Catalog (browse/detail/CRUD) | `epic:core-catalog` | ✅ Done — pre-dates formal sprint tracking |
| Auth & Roles | `epic:auth` | ✅ Done for password auth ([PR #7](https://github.com/pmconnolly80/FinalCapstone/pull/7); registration bug [#17](https://github.com/pmconnolly80/FinalCapstone/issues/17) fixed in [PR #20](https://github.com/pmconnolly80/FinalCapstone/pull/20) — password policy now explicit length-only min 8) — 🔵 social sign-in + password reset groomed into Sprint 4 ([#40](https://github.com/pmconnolly80/FinalCapstone/issues/40)–[#46](https://github.com/pmconnolly80/FinalCapstone/issues/46), 2026-07-21), see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6 |
| **Mug Club Progress & Bartender Confirmation** | `epic:mug-club` | ✅ **Done** — Sprint 1 ([PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11), 2026-07-14) + Sprint 2 (PRs [#21](https://github.com/pmconnolly80/FinalCapstone/pull/21)–[#25](https://github.com/pmconnolly80/FinalCapstone/pull/25), closed 2026-07-15). Built to the one-device rule: confirmation on the customer's phone, sealed by the bartender's personal 6-digit PIN, hardened with two-axis lockout, real PIN lifecycle, durable mug awards, and the admin correction path |
| **Customer Phone Experience** (search-first UX, availability states for the rotating inventory, Open Brewery DB brewery enrichment, Catalog.beer pre-fill, mobile repair) | `epic:phone-experience` | ✅ **Done** — Sprint 3 ([#26](https://github.com/pmconnolly80/FinalCapstone/issues/26)–[#32](https://github.com/pmconnolly80/FinalCapstone/issues/32), groomed 2026-07-20, closed 2026-07-21, PRs #33–#39). First slice pulled forward 2026-07-14 as a Sprint 2 interrupt ([#18](https://github.com/pmconnolly80/FinalCapstone/issues/18), landing-page facelift). 🔵 A follow-up polish pass (mobile nav, login screen, tab navigation) was flagged from 2026-07-23 live testing — see **Mobile UI Polish** below |
| Admin Experience (dashboard + anomaly panel, user/role/PIN mgmt UI, full data correction with audit, catalog bulk-add guardrail) | `epic:admin` | ✅ **Done** for the original scope — Sprint 2's first slice (confirmation audit/correction API + screen #15/#16, admin PIN issue/reset/deactivate API #13, mug-earner list #14) plus Sprint 5 ([#53](https://github.com/pmconnolly80/FinalCapstone/issues/53)–[#59](https://github.com/pmconnolly80/FinalCapstone/issues/59), groomed/closed 2026-07-23 — PRs [#60](https://github.com/pmconnolly80/FinalCapstone/pull/60)–[#66](https://github.com/pmconnolly80/FinalCapstone/pull/66)). 🔵 Four more UX-gap issues groomed into **Sprint 8** below ([#75](https://github.com/pmconnolly80/FinalCapstone/issues/75)–[#78](https://github.com/pmconnolly80/FinalCapstone/issues/78)) |
| Engagement, Retention & Social (badges, push notifications + owner composer, My Beers — ratings/want list/personal stats viz, social feed/cheers/leaderboard, journal, owner analytics) | `epic:retention` | ⬜ Not started — the business-owner payoff, see `FEATURE_MAP.md` and `PERSONAS_AND_USAGE.md`. 🔵 One pull-forward story groomed into **Sprint 8** below ([#74](https://github.com/pmconnolly80/FinalCapstone/issues/74): rating prompt + minimal milestone) |
| **Mobile UI Polish** (nav bar redesign for phone-first use + hidden pre-login, minimal branded login screen, several small UX-gap fixes) | `epic:ui-polish` | 🔵 Groomed 2026-07-23 as **Sprint 6** ([milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/6), issues [#67](https://github.com/pmconnolly80/FinalCapstone/issues/67)–[#71](https://github.com/pmconnolly80/FinalCapstone/issues/71)), not yet started |
| **Beer Discovery & Recommendations** (customer-facing external beer-database search, beer recommendations/requests + admin triage) | `epic:beer-discovery` | 🟡 Built 2026-07-23 — Sprint 7 ([#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)–[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73), [#83](https://github.com/pmconnolly80/FinalCapstone/issues/83)), PR open pending review/merge |
| Deployment & Hardening (AWS, CI/CD) | `epic:deployment` | ⬜ Not started — also covers wiring real SMTP credentials so forgot-password emails actually send (currently silently no-ops, flagged 2026-07-23) |
| Future Enhancements (public reviews, images) | `epic:future-enhancements` | ⬜ Backlog, unscheduled |

## Sprints

### Sprint 1: Mug Club Core — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/1) (✅ complete — merged 2026-07-14)

The core mug-club loop end to end: a bartender can confirm a beer for a customer, and that
customer can see their progress. This alone delivers the primary MVP driver described in
`PROJECT_PLAN.md` and `FEATURE_MAP.md`.

> **Status (2026-07-14):** [PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11)
> merged to `master` (37 backend / 38 frontend tests, CI green, verified live against
> Docker Postgres — see `SESSION_LOG.md`). Issues #2–#6 closed by the merge; milestone
> closed. #3/#6 had been re-titled on GitHub to the one-device PIN design before
> implementation.

1. [#2 Add Tavern and BeerConfirmation entities + migration](https://github.com/pmconnolly80/FinalCapstone/issues/2)
2. [#3 API: confirmation endpoint — customer session + bartender PIN](https://github.com/pmconnolly80/FinalCapstone/issues/3)
3. [#4 API: customer mug-club progress endpoint](https://github.com/pmconnolly80/FinalCapstone/issues/4)
4. [#5 UI: customer "My Progress" screen](https://github.com/pmconnolly80/FinalCapstone/issues/5)
5. [#6 UI: Confirmation PIN Pad on the customer's phone](https://github.com/pmconnolly80/FinalCapstone/issues/6)

### Sprint 2: Mug Club Completion — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/2) (✅ complete — closed 2026-07-15)

Finishes the epic on top of Sprint 1's verified core loop: brute-force protection for the
PIN-on-customer's-phone model, real PIN lifecycle, a durable mug-earned milestone, and the
admin fix path for at-the-bar mistakes (the first slice of admin edit-everything):

> **Status (2026-07-15):** all 7 items done — #12 ([PR #21](https://github.com/pmconnolly80/FinalCapstone/pull/21)),
> #13 ([PR #22](https://github.com/pmconnolly80/FinalCapstone/pull/22)), #14
> ([PR #23](https://github.com/pmconnolly80/FinalCapstone/pull/23)), #15
> ([PR #24](https://github.com/pmconnolly80/FinalCapstone/pull/24)), #16
> ([PR #25](https://github.com/pmconnolly80/FinalCapstone/pull/25)) plus interrupts
> #17/#18 (PRs #20/#19). Suites at close: backend 85/85, frontend 61/61, CI green on
> every merge; each story live-verified against the Docker stack. Milestone closed.
> Mug-earned permanence decision documented in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1.

1. [#12 API: PIN lockout — per-PIN and per-customer failed-attempt lockout](https://github.com/pmconnolly80/FinalCapstone/issues/12)
   — wires up the `FailedAttempts`/`LockedUntil` columns `StaffPin` shipped with
2. [#13 PIN lifecycle — admin issue/reset/deactivate, staff change their own](https://github.com/pmconnolly80/FinalCapstone/issues/13)
3. [#14 Mug earned — persist the milestone, surface it to customer and owner](https://github.com/pmconnolly80/FinalCapstone/issues/14)
4. [#15 API: admin confirmation audit & correction with required reason notes](https://github.com/pmconnolly80/FinalCapstone/issues/15)
5. [#16 UI: admin confirmation review & correction screen](https://github.com/pmconnolly80/FinalCapstone/issues/16)

Push notifications and badges are explicitly *not* here — they stay in the Engagement,
Retention & Social epic; #14 is the durable flag + in-app surfacing only.

**Interrupts (added 2026-07-14 from live testing — both ✅ done same day):**

- [#17 Bug: registration fails silently — API error discarded, password rules unhinted](https://github.com/pmconnolly80/FinalCapstone/issues/17)
  (`bug` + `epic:auth`) — ✅ [PR #20](https://github.com/pmconnolly80/FinalCapstone/pull/20):
  auth API errors surfaced in the UI, password policy now explicit length-only min 8
- [#18 UI: landing page facelift — adopt Tailwind, restyle app shell + home](https://github.com/pmconnolly80/FinalCapstone/issues/18)
  (`epic:phone-experience`) — ✅ [PR #19](https://github.com/pmconnolly80/FinalCapstone/pull/19):
  Tailwind v4 adopted, real Home page; the full progress-as-home screen and app-wide restyle
  stay in the Customer Phone Experience sprint

### Sprint 3: Customer Phone Experience — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/3) (✅ complete — closed 2026-07-21)

The July 2026 UX re-plan: makes the app actually live on the customer's phone rather than
being an aspirational app shell. Search-first beer list, availability states for the
rotating inventory, beer-nerd stats, Open Brewery DB enrichment, a Catalog.beer pre-fill
spike, and the mobile UX blockers found in the July 2026 code audit.

> **Status (2026-07-21):** all 7 items done — #26 ([PR #33](https://github.com/pmconnolly80/FinalCapstone/pull/33)),
> #27 ([PR #34](https://github.com/pmconnolly80/FinalCapstone/pull/34)), #28
> ([PR #35](https://github.com/pmconnolly80/FinalCapstone/pull/35)), #29
> ([PR #36](https://github.com/pmconnolly80/FinalCapstone/pull/36)), #30
> ([PR #37](https://github.com/pmconnolly80/FinalCapstone/pull/37)), #31
> ([PR #38](https://github.com/pmconnolly80/FinalCapstone/pull/38)), #32
> ([PR #39](https://github.com/pmconnolly80/FinalCapstone/pull/39)). Search-first beer list
> with availability/had-not-had filters and quick-search chips; beer-nerd stats (ABV/IBU/
> style family/class) plus Open Brewery DB brewery cards and admin autocomplete (the
> backend's first two external-API integrations, both cached and failure-tolerant); a real
> hit-rate spike against Catalog.beer (**GO**) with the pre-fill wired in the same story;
> and a mobile UX repair pass — reactive auth-aware nav, a progress-centric home for
> signed-in customers, beer CRUD actually gated off the customer surface (not just its nav
> link), the hardcoded-`localhost` API URL fixed (verified live over a LAN IP), and visible
> error states replacing the last `console.error`-only failure paths. Suites at close:
> backend 131/131, frontend 99/99; every story live-verified against the Docker stack,
> several against real external APIs. Along the way (#28), corrected the `verify` skill's
> stale claim that the frontend container is volume-mounted — it isn't, so frontend edits
> need `docker compose up -d --build web` like backend ones do. Full per-story detail in
> `SESSION_LOG.md`'s 2026-07-21 entries.

1. [#26 Data: Beer.Availability state (on tap / available / out of stock / retired)](https://github.com/pmconnolly80/FinalCapstone/issues/26)
2. [#27 API: beer search endpoint (name/brewery/style, paginated, availability + had/not-had filters)](https://github.com/pmconnolly80/FinalCapstone/issues/27)
   — depends on #26
3. [#28 UI: search-first beer list (autocomplete, filter chips, confirmed checkmark + availability badge)](https://github.com/pmconnolly80/FinalCapstone/issues/28)
   — depends on #27
4. [#29 Beer detail: beer-nerd stats (ABV, IBU, style family/class) + Open Brewery DB brewery card](https://github.com/pmconnolly80/FinalCapstone/issues/29)
5. [#30 Admin: Open Brewery DB brewery autocomplete in beer add/edit form](https://github.com/pmconnolly80/FinalCapstone/issues/30)
   — depends on #29 (shares its OBDB caching service)
6. [#31 Catalog.beer beer-level pre-fill spike (hit-rate spike, go/no-go, admin pre-fill if go)](https://github.com/pmconnolly80/FinalCapstone/issues/31)
   — go decision, integration wired in the same story
7. [#32 Mobile UX repair: progress-centric home, auth-aware nav, remove customer CRUD, fix hardcoded API URL, error/loading states](https://github.com/pmconnolly80/FinalCapstone/issues/32)

OBDB is breweries-only (no beer-level endpoint) — the tavern's list stays the source of
truth for beers; data-sourcing principle is auto-enrich from open projects so staff never
have to type beer data, manual entry as fallback/override. See `MVP_SCREEN_PLAN.md` and
`TECHNICAL_ARCHITECTURE_PLAN.md` §6 for the Catalog.beer research.

### Sprint 4: Auth II — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/4) (✅ complete — #40–#46 merged 2026-07-23)

Social sign-in (Google/Facebook/Apple) via ASP.NET Core Identity external login providers,
account linking on verified email, marketing-consent capture, a privacy policy page +
data-deletion path, and password reset — the app's first email-delivery dependency.

1. [#40 Data: ApplicationUser custom Identity user + marketing-consent field](https://github.com/pmconnolly80/FinalCapstone/issues/40)
2. [#41 API: pluggable email sender (IEmailSender + SMTP)](https://github.com/pmconnolly80/FinalCapstone/issues/41)
3. [#42 API + UI: forgot/reset password flow](https://github.com/pmconnolly80/FinalCapstone/issues/42)
   — depends on #41
4. [#43 API: Google external sign-in](https://github.com/pmconnolly80/FinalCapstone/issues/43)
5. [#44 API: Facebook external sign-in + privacy policy page + data-deletion path](https://github.com/pmconnolly80/FinalCapstone/issues/44)
   — depends on #40
6. [#45 API: Apple (Sign in with Apple) external sign-in](https://github.com/pmconnolly80/FinalCapstone/issues/45)
7. [#46 UI: social sign-in buttons + account-linking screen + marketing-consent checkbox](https://github.com/pmconnolly80/FinalCapstone/issues/46)
   — depends on #40, #43, #44, #45

Approach decided in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6: ASP.NET Core Identity external
login providers (not a hosted vendor), callback links-or-creates a local user matched on
verified email, the API keeps issuing its own JWT via `AuthController`'s existing
`CreateToken` — same as password login today.

### Sprint 5: Admin Experience — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/5) (✅ complete — #53–#59 merged 2026-07-23)

Admin dashboard with an anomaly panel, a beer management table (catalog CRUD's only
home, replacing the last customer-surface remnants), user/role management + bartender
PIN admin UI (role assignment has been DB-manual only, see `CLAUDE.md`), and a
generalized audited data-correction path extending Sprint 2's `ConfirmationAudit`
pattern (#15/#16) to beers and accounts. Social-content moderation stays out of scope
until the Engagement & Social epic actually ships a social layer to moderate.

1. [#53 Data: generalized AdminAudit trail + role assignment API](https://github.com/pmconnolly80/FinalCapstone/issues/53)
   — done, [PR #60](https://github.com/pmconnolly80/FinalCapstone/pull/60) (merged)
2. [#54 API: user management + account actions](https://github.com/pmconnolly80/FinalCapstone/issues/54)
   — depends on #53 — done, [PR #61](https://github.com/pmconnolly80/FinalCapstone/pull/61) (merged)
3. [#55 UI: User Management screen](https://github.com/pmconnolly80/FinalCapstone/issues/55)
   — depends on #54 (and #53 for role assignment) — done, [PR #62](https://github.com/pmconnolly80/FinalCapstone/pull/62) (merged)
4. [#56 API: audited beer edit/delete + inline availability update](https://github.com/pmconnolly80/FinalCapstone/issues/56)
   — depends on #53 — done, [PR #63](https://github.com/pmconnolly80/FinalCapstone/pull/63) (merged)
5. [#57 UI: Beer Management Table (admin)](https://github.com/pmconnolly80/FinalCapstone/issues/57)
   — depends on #56 — done, [PR #64](https://github.com/pmconnolly80/FinalCapstone/pull/64) (merged)
6. [#58 API: anomaly detection (bulk add / confirmation velocity / off-hours)](https://github.com/pmconnolly80/FinalCapstone/issues/58)
   — independent, surfaced in #59 — done, [PR #65](https://github.com/pmconnolly80/FinalCapstone/pull/65) (merged)
7. [#59 UI: Admin Dashboard](https://github.com/pmconnolly80/FinalCapstone/issues/59)
   — depends on #55, #57, #58 — done, [PR #66](https://github.com/pmconnolly80/FinalCapstone/pull/66) (open) — closes the sprint

Approach: `AdminAudit` (#53) mirrors `ConfirmationAudit`'s shape rather than replacing
it — confirmations keep their own existing audit trail from Sprint 2. Anomaly detection
(#58) is informational only, never blocking, per `IMPLEMENTATION_BACKLOG.md` Phase 5.

### Later sprints (named only — groomed into issues when they're next up)
- **Engagement, Retention & Social** — milestone badges (25/50/100/150), push
  notification infrastructure (PWA + service worker + `PushSubscription` + VAPID),
  automated sends (new beers / nudges / win-back) and the owner's composer with
  consent-gated audience targeting, social layer v1 (opt-in display name, milestone
  activity feed, cheers, leaderboard, communal goal widget, wall of mugs), seasonal
  mini-challenges, beer journal (tasting notes), owner analytics + marketing segments.
  **My Beers (added 2026-07-14)**: completed list with dates + 1–5 ratings on confirmed
  beers (rate-after-confirm prompt on the PIN pad success state), want list with
  in-stock-tonight filter, auto-check-off, and on-tap push trigger, My Stats
  visualizations (`GET /api/me/stats`), owner want-demand + anonymized-rating aggregates
  (see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.7)
- **Deployment & Hardening** — AWS deployment per `beer-app/infra/aws-architecture.md`, CI/CD,
  plus the security fixes from the July 2026 audit (committed JWT signing key, wide-open
  CORS, no admin/bartender account bootstrap)
- **Future Enhancements** — public reviews/ratings, images, recommendations

### Sprint 6: Mobile UI Polish — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/6) (groomed 2026-07-23, closed 2026-07-23 — [PR #84](https://github.com/pmconnolly80/FinalCapstone/pull/84), merged)

Phone-first nav and login-screen fixes plus several small UX gaps, all surfaced by
2026-07-23 live/usability testing (full findings in `USABILITY_TESTING.md`).

1. [#67 UI: replace top nav with a bottom tab bar, hidden until signed in](https://github.com/pmconnolly80/FinalCapstone/issues/67) — done
2. [#68 UI: minimal branded login screen (placeholder logo, pending color theme)](https://github.com/pmconnolly80/FinalCapstone/issues/68) — done
3. [#69 UI: bartender-visible signal after repeated PIN confirmation failures](https://github.com/pmconnolly80/FinalCapstone/issues/69) — done
4. [#70 UI: first-visit empty-state hint on beer search for 0-confirmation customers](https://github.com/pmconnolly80/FinalCapstone/issues/70) — done
5. [#71 UI: graceful offline/no-signal message on confirmation failure](https://github.com/pmconnolly80/FinalCapstone/issues/71) — done
6. [#82 UI: Account/Profile hub screen](https://github.com/pmconnolly80/FinalCapstone/issues/82) — done
   (added 2026-07-23 coverage review) — depends on/delivered alongside #67, since the
   bottom tab bar's "Account" tab needs somewhere to actually send My PIN, Linked
   Accounts, Privacy Policy, and Sign Out

Approach: all 6 issues shipped in one PR since each is small and touches overlapping
UI (the tab bar and the Account hub are two halves of the same change). No backend
changes — a pure frontend sprint. Frontend suite (158/158) and backend suite
(239/239, unaffected) both pass, plus a clean `npm run build`. Manually verified with
a real Playwright-driven Chromium session at a phone-sized viewport before merging
(not just RTL tests) — confirmed the nav is absent when signed out, the bottom tab
bar and Account hub render correctly for both a fresh customer and the seeded admin
account, the BeerList first-visit hint, and both the offline and repeated-PIN-failure
messages on the confirmation PIN pad. #71 didn't auto-close from the merge commit's
closing keywords (closed manually, cross-referencing PR #84).

### Sprint 7: Beer Discovery & Recommendations — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/7) (groomed 2026-07-23, built 2026-07-23)

Customer-facing external beer-database search (distinct from the tavern's-own-list
search) plus a customer beer-recommendation/request feature with admin triage.

1. [#72 API + UI: customer-facing external beer database search](https://github.com/pmconnolly80/FinalCapstone/issues/72) —
   acceptance criteria amended 2026-07-23 to require sign-in + a rate limit before
   this reaches the API-keyed Catalog.beer dependency
2. [#73 API + UI: customer beer recommendations/requests + admin triage](https://github.com/pmconnolly80/FinalCapstone/issues/73)
   — depends on #72 for the "recommend from a search hit" path, but stands alone for
   plain-text recommendations
3. [#83 Admin: external-search demand report](https://github.com/pmconnolly80/FinalCapstone/issues/83)
   (added 2026-07-23 coverage review) — depends on #72; surfaces what #72 logs
   (searched-but-not-carried beers) since nothing else in this sprint displays it

> **Status (2026-07-23):** all three stories built in one pass, in dependency order
> (#72 → #73 → #83). New `BeerLookupController` (`GET /api/beer-lookup/search`) reuses
> the existing `ICatalogBeerService`/`IBreweryLookupService` behind a signed-in-only,
> per-user-rate-limited endpoint (ASP.NET Core's built-in `RateLimiter`, 20 req/min via
> a fixed-window policy partitioned by the caller's user id) and logs every call to a
> new `ExternalSearchLog` table (query text + whether it matched the tavern's own
> catalog). `BeerList.jsx` grew a "What's on our list" / "Look up any beer" toggle
> (signed-in only) so the two search modes stay visually distinct. New
> `RecommendationsController`/`AdminRecommendationsController` give customers a
> `BeerRecommendation` submission path (plain-text or pre-filled from a lookup hit) and
> admins a filterable triage screen (`/admin/recommendations`, status
> New/Reviewed/Added/Declined, no reason required — closer to the availability PATCH's
> immediate toggle than confirmation-void's reason guard). New
> `AdminExternalSearchController` (`/admin/search-demand`) aggregates unmatched
> `ExternalSearchLog` rows by frequency, closing the loop #83 flagged. Suites: backend
> 271/271 (+32), frontend 175/175 (+17), clean `npm run build`. Verified live against
> the Docker stack: real Catalog.beer/Open Brewery DB results for "duvel", a plain-text
> and a search-hit recommendation both submitted and triaged to `Added` by the seeded
> admin, the demand report showing an unmatched query, the tavern's own catalog search
> unaffected, and the rate limit tripping to 429 on the 20th+ request in a window.

### Sprint 8: Admin & Engagement UX Follow-ups — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/8) (groomed 2026-07-23, not started)

Small admin/engagement UX-gap fixes from the 2026-07-23 review, extending
`epic:retention`/`epic:admin` ahead of those epics' full grooming — bundled together
since each is small and independent rather than because they share one epic, per
explicit user direction to narrow priorities now rather than wait for strict
sequencing.

1. [#74 UI + API: pull forward 'How was it?' rating prompt + minimal milestone moment](https://github.com/pmconnolly80/FinalCapstone/issues/74) (`epic:retention`) —
   acceptance criteria amended 2026-07-23 to add a rating view/edit affordance on beer
   detail, since My Beers doesn't exist yet
2. [#75 UI: inline consequence microcopy on audited admin actions](https://github.com/pmconnolly80/FinalCapstone/issues/75) (`epic:admin`)
3. [#76 UI: staff-only filter + search on the User Management table](https://github.com/pmconnolly80/FinalCapstone/issues/76) (`epic:admin`)
4. [#77 API + UI: admin-initiated bartender account invite](https://github.com/pmconnolly80/FinalCapstone/issues/77) (`epic:admin`)
5. [#78 UI + API: reframe Admin Dashboard as operational health; pull forward most/least-confirmed beers](https://github.com/pmconnolly80/FinalCapstone/issues/78) (`epic:admin`)
6. [#79 API + UI: support variable-length staff PINs (e.g. birthday format)](https://github.com/pmconnolly80/FinalCapstone/issues/79) (`epic:admin`) —
   acceptance criteria amended 2026-07-23 to explicitly call out 3 files with hardcoded
   "6-digit" copy beyond the validation logic
7. [#80 API + UI: mark a beer out-of-stock from the confirmation PIN pad](https://github.com/pmconnolly80/FinalCapstone/issues/80) (`epic:admin`) —
   acceptance criteria amended 2026-07-23 to support toggling back to available (not
   just out-of-stock) and to require a deliberate confirm step
8. [#81 API + UI: customer-facing 'flag beer as unavailable' report](https://github.com/pmconnolly80/FinalCapstone/issues/81) (`epic:admin`)

## Live Testing Findings — 2026-07-23

The user visited the deployed site and, over two rounds, reported seven initial issues
plus a deeper product/UX gap analysis of the customer and admin/owner experience — full
findings, decisions, and issue mapping now live in **[`USABILITY_TESTING.md`](USABILITY_TESTING.md)**
rather than duplicated here. Headlines: dev admin (`admin@tavern.local`/`admin1234`) and
test customer (`user1@gmail.com`/`1234User1#!`) accounts seeded this session
(`BeerApi.Tests/Data/SeedDataTests.cs` +3 tests, backend suite 239/239 at the time);
forgot-password confirmed as an SMTP-config gap, not a bug; twelve real product/UX gaps
found and resolved into the Sprint 6/7/8 issues above plus two areas flagged as needing
more architecture design before they can be ticketed (bartender account model, mid-shift
availability permissions — see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1).

**Tracking-drift fix (2026-07-23):** Milestone 4 ("Sprint 4: Auth II") was marked closed
in GitHub while issues #40–#46 were still open. Verified each against its closing PR
(#47–#52, all confirmed merged and referencing the correct issue number in the PR body)
before closing all 7 — genuinely done, just never auto-closed because the PRs said
"#40:" rather than "Closes #40."

## Session traceability

Every working session gets an entry in [`SESSION_LOG.md`](SESSION_LOG.md) noting which
sprint/story it touched. That, plus each issue/PR referencing the commits that closed it, is
how a reader can confirm the work actually stayed in line with this plan rather than drifting.
