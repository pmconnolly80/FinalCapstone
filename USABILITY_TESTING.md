# Usability Testing

A living log of usability/product-testing rounds against the running app, distinct from
`POST_MORTEM.md` (a one-time Sprint 1–5 build retrospective, not updated in place). New
rounds get appended here as new dated sections. See `EPICS_AND_SPRINTS.md`'s epics table
and "Live Testing Findings" section for how each finding below maps to backlog issues,
and `SESSION_LOG.md` for the session that produced each round.

---

## Round 1 — 2026-07-23: first live site visit

The user visited the deployed site for the first time and reported seven issues in one
pass.

| # | Finding | Resolution |
|---|---|---|
| 1 | Forgot-password email never arrives | Not a code bug — SMTP is unconfigured everywhere (`appsettings.json`, `docker-compose.yml`, untracked `.env`); `SmtpEmailSender` correctly no-ops by design. Tracked under Deployment & Hardening. |
| 2 | Nav bar disliked; shouldn't show pre-login | Real gap — see Round 2 finding C1 below, now issue [#67](https://github.com/pmconnolly80/FinalCapstone/issues/67). |
| 3 | Login screen should be minimal, logo + login only | Now issue [#68](https://github.com/pmconnolly80/FinalCapstone/issues/68). |
| 4 | Top "tabs" don't make sense | Same root cause as #2 — addressed by the same nav redesign, issue #67. |
| 5 | Beer list "doesn't search the API" | Investigated — no code defect (debounce, params, endpoint all correct). Root cause was scope mismatch: customer expected search to reach the *external* beer database, while today's design deliberately only searches the tavern's own list. Turned into a real feature request — see [#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)/[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73). |
| 6 | Dev/testing admin account | Fixed same session — `SeedData.cs` seeds `admin@tavern.local` / `admin1234` (`Admin` role). (`admin`/`admin` as literally requested doesn't work: the app has no username-only login, and the password fails the app's own min-8-char policy.) |
| 7 | Dev/testing customer account | Fixed same session — `SeedData.cs` seeds `user1@gmail.com` / `1234User1#!` (`Customer` role). |

Regression coverage: `BeerApi.Tests/Data/SeedDataTests.cs` gained 3 tests. Backend suite:
239/239 at the time.

---

## Round 2 — 2026-07-23: product/UX gap analysis

Prompted by the user's own assessment that, despite five sprints all marked "done," the
app "does not fulfill its goals" for real users yet. Rather than only chase the Round 1
symptoms, a deeper pass stress-tested the customer and admin/owner experience against
`PROJECT_PLAN.md`, `PERSONAS_AND_USAGE.md`, `FEATURE_MAP.md`, `TECHNICAL_ARCHITECTURE_PLAN.md`
§4.1, and the actual shipped code — looking for places where the implemented flow doesn't
achieve what the planning docs promise, or where the docs themselves have an unresolved gap.

### Customer Experience

| # | Gap | Decision |
|---|---|---|
| C1 | No acquisition story — nothing specifies how a customer discovers the app the first time | QR code (table-tent/coaster, physical/marketing) → in-app `/auth?mode=register` entry point, for v1. Native app-store presence (Google Play / Apple App Store) is a real future direction — logged as an unscoped backlog candidate, not yet designed. |
| C2 | No offline/dead-phone fallback — a customer with no signal can't get confirmed that night | **Accepted as out of scope for v1.** No backfill mechanism built; just a graceful in-app message ("no signal — ask the bartender to note it"). Issue [#71](https://github.com/pmconnolly80/FinalCapstone/issues/71). Resolves `PERSONAS_AND_USAGE.md` §7.5's long-open question. |
| C3 | Generic 401 on every confirmation failure means a bartender can't tell "wrong PIN" from "locked out 15 min" | Bartender-visible client-side signal after N consecutive failures, without revealing the actual cause to the customer holding the phone. Issue [#69](https://github.com/pmconnolly80/FinalCapstone/issues/69). |
| C4 | The promised engagement "hook" (badges/ratings/social) doesn't exist — the loop ends at a bare number | Pull two pieces forward ahead of the full Engagement epic: a minimal milestone moment (e.g. 100 beers) AND the "How was it?" rating prompt. Issue [#74](https://github.com/pmconnolly80/FinalCapstone/issues/74). |
| C5 | Repeat confirmations mid-visit are a full-screen modal every time, no continuous flow | **Left as-is for now** — noted as a real gap vs. the persona narrative, not built against yet. |
| C6 | A brand-new customer's had/not-had filters are useless on night one (0 confirmations) | First-visit empty-state hint when confirmed count is 0. Issue [#70](https://github.com/pmconnolly80/FinalCapstone/issues/70). |

### Admin/Owner Experience

| # | Gap | Decision |
|---|---|---|
| A1 | No admin-initiated way to onboard a new bartender — only self-registration exists | Admin-initiated invite (creates the account, emails a set-password link) against today's account model. Issue [#77](https://github.com/pmconnolly80/FinalCapstone/issues/77). **Separately flagged**: the user floated a lighter future model where bartenders never get a real account at all — admin creates a staff record + PIN directly, using the bartender's birthday (`MMDDYYYY`, 8 digits) as an easy-to-remember PIN. Documented, not decided — see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1's "Open architecture questions." |
| A2 | Nobody physically at the bar can mark a keg as kicked mid-shift — only Admin can, conflicting with the one-device rule | No single decision — user wants elements of all three explored: (a) a narrow bartender availability-only permission, (b) house policy (bartender texts/calls the admin), (c) a customer-facing "flag as unavailable" report. **Real open tension**: (a) requires bartenders to be authenticated users, which conflicts with A1's lighter future account model. Documented in `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 as unresolved — needs the account-model question settled first. Not yet ticketed. |
| A3 | The shipped Admin Dashboard doesn't answer the owner's real question ("what to order," "who's lapsing") | Reframe the dashboard's purpose explicitly as "operational health," with beer intelligence deferred to a separate future Owner Analytics screen; pull the cheap most/least-confirmed-beers query forward as a fast follow. Issue [#78](https://github.com/pmconnolly80/FinalCapstone/issues/78). |
| A4 | `PERSONAS_AND_USAGE.md` says Owner/Admin roles should stay separable; the code has already merged them | Not a strict permission split after all — decided direction is **multiple individually-attributed Admin accounts** (mostly already true via `AdminAudit`) **plus one top-level account that can provision the others**. Documented in `PERSONAS_AND_USAGE.md`, not yet designed/ticketed — needs a real architecture pass (new role tier vs. a flag/claim on Admin). |
| A5 | Audit "reason" fields are bare free-text with no in-UI explanation of real consequences | Inline consequence microcopy at the point of each audited action. Issue [#75](https://github.com/pmconnolly80/FinalCapstone/issues/75). |
| A6 | User Management table has no search/filter — lists every customer, not just staff | Staff-only filter (default) plus a search box. Issue [#76](https://github.com/pmconnolly80/FinalCapstone/issues/76). |

### What got groomed into real GitHub issues/milestones this session

- **Milestone 6 — Mobile UI Polish**: issues [#67](https://github.com/pmconnolly80/FinalCapstone/issues/67)–[#71](https://github.com/pmconnolly80/FinalCapstone/issues/71) (`epic:ui-polish`)
- **Milestone 7 — Beer Discovery & Recommendations**: issues [#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)–[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73) (`epic:beer-discovery`)
- **Unassigned to a milestone yet** (sequencing not decided): [#74](https://github.com/pmconnolly80/FinalCapstone/issues/74) (`epic:retention`, engagement pull-forward), [#75](https://github.com/pmconnolly80/FinalCapstone/issues/75)–[#78](https://github.com/pmconnolly80/FinalCapstone/issues/78) (`epic:admin`, admin UX polish)

This deliberately deviates from the repo's usual "only the next epic gets ticketed"
convention (`EPICS_AND_SPRINTS.md`) — the user explicitly asked to groom multiple
candidate epics now, in order to narrow down priorities, rather than wait for strict
epic sequencing.

### What's documented but NOT ticketed — needs more design first

- Bartender account model reconsideration (birthday PIN, no real Identity account) — `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1
- Mid-shift availability permission model, blocked on the above — `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1
- Multi-Admin-account + provisioning-tier model — `PERSONAS_AND_USAGE.md`
- Native app-store distribution — mentioned above under C1, no design started

### Known doc/GitHub drift found while grooming (not fixed this session)

Milestone 4 ("Sprint 4: Auth II") is marked **closed** in GitHub, but issues #40–#46 are
all still **open** — `EPICS_AND_SPRINTS.md` describes Sprint 4 as fully merged and closed
2026-07-23, which the code/PR history supports, but the underlying GitHub issues were
never closed to match. Flagged for a future cleanup pass, not corrected here since it
needs verification against each PR before bulk-closing issues.
