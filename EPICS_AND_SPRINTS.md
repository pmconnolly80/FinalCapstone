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
| Auth & Roles | `epic:auth` | ✅ Done for password auth ([PR #7](https://github.com/pmconnolly80/FinalCapstone/pull/7); registration bug [#17](https://github.com/pmconnolly80/FinalCapstone/issues/17) fixed in [PR #20](https://github.com/pmconnolly80/FinalCapstone/pull/20) — password policy now explicit length-only min 8) — **new scope added July 2026, not yet ticketed**: social sign-in (Google/Facebook/Apple via Identity external providers) + marketing-consent capture, see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6; the gap was re-confirmed by 2026-07-14 live testing (no ticket yet per the grooming rule) |
| **Mug Club Progress & Bartender Confirmation** | `epic:mug-club` | ✅ **Done** — Sprint 1 ([PR #11](https://github.com/pmconnolly80/FinalCapstone/pull/11), 2026-07-14) + Sprint 2 (PRs [#21](https://github.com/pmconnolly80/FinalCapstone/pull/21)–[#25](https://github.com/pmconnolly80/FinalCapstone/pull/25), closed 2026-07-15). Built to the one-device rule: confirmation on the customer's phone, sealed by the bartender's personal 6-digit PIN, hardened with two-axis lockout, real PIN lifecycle, durable mug awards, and the admin correction path |
| Customer Phone Experience (search-first UX, availability states for the rotating inventory, Open Brewery DB brewery enrichment, mobile repair) | `epic:phone-experience` | ⬜ Not started — planned July 2026, see `MOBILE_FIRST_PRODUCT_OUTLINE.md`. First slice pulled forward 2026-07-14 as a Sprint 2 interrupt ([#18](https://github.com/pmconnolly80/FinalCapstone/issues/18), landing-page facelift) |
| Admin Experience (dashboard + anomaly panel, user/role/PIN mgmt UI, full data correction with audit, catalog bulk-add guardrail) | `epic:admin` | 🔵 First slice shipped with Sprint 2 (confirmation audit/correction API + screen #15/#16, admin PIN issue/reset/deactivate API #13, mug-earner list #14) — dashboard, anomaly panel, and user/role management UI still to come |
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

### Later sprints (named only — groomed into issues when they're next up)

- **Customer Phone Experience** — the July 2026 UX re-plan: search-first beer list
  (API search/pagination, autocomplete, had/not-had filter), availability states for the
  rotating inventory (on tap / out of stock / retired; in-stock default; confirmations
  permanent through inventory churn), beer detail built for beer nerds — ABV, IBU, style
  family/class, description, plus Open Brewery DB brewery data (OBDB is breweries-only;
  the tavern's list stays the source of truth). Data-sourcing principle: auto-enrich from
  open projects so staff never have to type beer data, manual entry as fallback/override
  — includes the **Catalog.beer hit-rate spike** (researched 2026-07-14, the beer-level
  candidate; beer.db evaluated and rejected as dormant, see
  `TECHNICAL_ARCHITECTURE_PLAN.md` §6). Also: progress-centric home screen, auth-aware
  navigation, CRUD removed from the customer surface, and the mobile blockers found in
  the July 2026 code audit (hardcoded `localhost` API URL, error states, form usability)
- **Auth II: Social Sign-in** — Google/Facebook/Apple via ASP.NET Core Identity external
  login providers (researched July 2026, `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6),
  account linking on verified email, marketing-consent capture, privacy policy +
  data-deletion path. **Password reset** (added to scope 2026-07-14): Identity's built-in
  reset tokens + a "forgot password" flow; brings in the app's first email-sender
  dependency (SMTP/SES) — see `IMPLEMENTATION_BACKLOG.md` Phase 3
- **Admin Experience** — admin dashboard with anomaly panel (bulk beer-add alerts to
  owner+admin, confirmation velocity spikes, off-hours activity), beer management table
  (catalog CRUD's new home, with OBDB brewery autocomplete and inline availability),
  user/role management UI (role assignment is currently DB-manual only, see `CLAUDE.md`)
  including bartender PIN management, and **full data correction** — admin can edit any
  record (beers, confirmations, accounts, social content) with required reason notes and
  an audit trail
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
