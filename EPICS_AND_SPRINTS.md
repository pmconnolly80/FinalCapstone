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
| **Customer Phone Experience** (search-first UX, availability states for the rotating inventory, Open Brewery DB brewery enrichment, Catalog.beer pre-fill, mobile repair) | `epic:phone-experience` | ✅ **Done** — Sprint 3 ([#26](https://github.com/pmconnolly80/FinalCapstone/issues/26)–[#32](https://github.com/pmconnolly80/FinalCapstone/issues/32), groomed 2026-07-20, closed 2026-07-21, PRs #33–#39). First slice pulled forward 2026-07-14 as a Sprint 2 interrupt ([#18](https://github.com/pmconnolly80/FinalCapstone/issues/18), landing-page facelift) |
| Admin Experience (dashboard + anomaly panel, user/role/PIN mgmt UI, full data correction with audit, catalog bulk-add guardrail) | `epic:admin` | 🔵 First slice shipped with Sprint 2 (confirmation audit/correction API + screen #15/#16, admin PIN issue/reset/deactivate API #13, mug-earner list #14) — Sprint 5 ([#53](https://github.com/pmconnolly80/FinalCapstone/issues/53)–[#59](https://github.com/pmconnolly80/FinalCapstone/issues/59), 2026-07-23) in progress: #53 done ([PR #60](https://github.com/pmconnolly80/FinalCapstone/pull/60) open), #54 done ([PR #61](https://github.com/pmconnolly80/FinalCapstone/pull/61) open) |
| Engagement, Retention & Social (badges, push notifications + owner composer, My Beers — ratings/want list/personal stats viz, social feed/cheers/leaderboard, journal, owner analytics) | `epic:retention` | ⬜ Not started — the business-owner payoff, see `FEATURE_MAP.md` and `PERSONAS_AND_USAGE.md` |
| Deployment & Hardening (AWS, CI/CD) | `epic:deployment` | ⬜ Not started |
| Future Enhancements (public reviews, images, recommendations) | `epic:future-enhancements` | ⬜ Backlog, unscheduled |

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

### Sprint 5: Admin Experience — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/5) (groomed 2026-07-23)

Admin dashboard with an anomaly panel, a beer management table (catalog CRUD's only
home, replacing the last customer-surface remnants), user/role management + bartender
PIN admin UI (role assignment has been DB-manual only, see `CLAUDE.md`), and a
generalized audited data-correction path extending Sprint 2's `ConfirmationAudit`
pattern (#15/#16) to beers and accounts. Social-content moderation stays out of scope
until the Engagement & Social epic actually ships a social layer to moderate.

1. [#53 Data: generalized AdminAudit trail + role assignment API](https://github.com/pmconnolly80/FinalCapstone/issues/53)
   — done, [PR #60](https://github.com/pmconnolly80/FinalCapstone/pull/60) (open)
2. [#54 API: user management + account actions](https://github.com/pmconnolly80/FinalCapstone/issues/54)
   — depends on #53 — done, [PR #61](https://github.com/pmconnolly80/FinalCapstone/pull/61) (open)
3. [#55 UI: User Management screen](https://github.com/pmconnolly80/FinalCapstone/issues/55)
   — depends on #54 (and #53 for role assignment)
4. [#56 API: audited beer edit/delete + inline availability update](https://github.com/pmconnolly80/FinalCapstone/issues/56)
   — depends on #53
5. [#57 UI: Beer Management Table (admin)](https://github.com/pmconnolly80/FinalCapstone/issues/57)
   — depends on #56
6. [#58 API: anomaly detection (bulk add / confirmation velocity / off-hours)](https://github.com/pmconnolly80/FinalCapstone/issues/58)
   — independent, surfaced in #59
7. [#59 UI: Admin Dashboard](https://github.com/pmconnolly80/FinalCapstone/issues/59)
   — depends on #55, #57, #58 — closes the sprint

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

## Session traceability

Every working session gets an entry in [`SESSION_LOG.md`](SESSION_LOG.md) noting which
sprint/story it touched. That, plus each issue/PR referencing the commits that closed it, is
how a reader can confirm the work actually stayed in line with this plan rather than drifting.
