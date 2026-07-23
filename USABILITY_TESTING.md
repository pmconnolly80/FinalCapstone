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
| A1 | No admin-initiated way to onboard a new bartender — only self-registration exists | **Decided 2026-07-23**: admin-initiated invite (creates the account, emails a set-password link), keeping today's full-Identity-account model — the lighter no-login alternative floated in Round 2 was considered and rejected. Issue [#77](https://github.com/pmconnolly80/FinalCapstone/issues/77). Bartenders can still get an easy-to-remember birthday-format (`MMDDYYYY`, 8-digit) PIN once PIN length is configurable — issue [#79](https://github.com/pmconnolly80/FinalCapstone/issues/79). |
| A2 | Nobody physically at the bar can mark a keg as kicked mid-shift — only Admin can, conflicting with the one-device rule | **Decided 2026-07-23**: layer all three. Primary — piggyback an availability flag onto the existing PIN-confirmation trust model (issue [#80](https://github.com/pmconnolly80/FinalCapstone/issues/80)), which resolves the original tension since it rides on PIN resolution rather than a role-based `[Authorize]` permission. Secondary — customer-facing crowd-sourced "flag as unavailable" report (issue [#81](https://github.com/pmconnolly80/FinalCapstone/issues/81)). Fallback — house policy (bartender texts/calls the admin), no code needed. |
| A3 | The shipped Admin Dashboard doesn't answer the owner's real question ("what to order," "who's lapsing") | Reframe the dashboard's purpose explicitly as "operational health," with beer intelligence deferred to a separate future Owner Analytics screen; pull the cheap most/least-confirmed-beers query forward as a fast follow. Issue [#78](https://github.com/pmconnolly80/FinalCapstone/issues/78). |
| A4 | `PERSONAS_AND_USAGE.md` says Owner/Admin roles should stay separable; the code has already merged them | Not a strict permission split after all — decided direction is **multiple individually-attributed Admin accounts** (mostly already true via `AdminAudit`) **plus one top-level account that can provision the others**. Documented in `PERSONAS_AND_USAGE.md`, not yet designed/ticketed — needs a real architecture pass (new role tier vs. a flag/claim on Admin). |
| A5 | Audit "reason" fields are bare free-text with no in-UI explanation of real consequences | Inline consequence microcopy at the point of each audited action. Issue [#75](https://github.com/pmconnolly80/FinalCapstone/issues/75). |
| A6 | User Management table has no search/filter — lists every customer, not just staff | Staff-only filter (default) plus a search box. Issue [#76](https://github.com/pmconnolly80/FinalCapstone/issues/76). |

### What got groomed into real GitHub issues/milestones

- **Milestone 6 — Mobile UI Polish**: issues [#67](https://github.com/pmconnolly80/FinalCapstone/issues/67)–[#71](https://github.com/pmconnolly80/FinalCapstone/issues/71) (`epic:ui-polish`)
- **Milestone 7 — Beer Discovery & Recommendations**: issues [#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)–[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73) (`epic:beer-discovery`)
- **Milestone 8 — Admin & Engagement UX Follow-ups**: issues [#74](https://github.com/pmconnolly80/FinalCapstone/issues/74)–[#81](https://github.com/pmconnolly80/FinalCapstone/issues/81) (`epic:retention`/`epic:admin`) — includes the two architecture-question follow-ons (#79, #80, #81) added in Round 3

This deliberately deviates from the repo's usual "only the next epic gets ticketed"
convention (`EPICS_AND_SPRINTS.md`) — the user explicitly asked to groom multiple
candidate epics now, in order to narrow down priorities, rather than wait for strict
epic sequencing.

### What's documented but NOT ticketed — needs more design first

- Multi-Admin-account + provisioning-tier model (A4 above) — `PERSONAS_AND_USAGE.md`
- Native app-store distribution — mentioned above under C1, no design started

## Round 3 — 2026-07-23: architecture decisions + tracking cleanup

Follow-up session picking up where Round 2 left off. Three things resolved:

1. **The two open architecture questions from Round 2 (A1/A2) are now decided** —
   see the updated A1/A2 rows above and `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1. Three
   new issues created: [#79](https://github.com/pmconnolly80/FinalCapstone/issues/79)
   (variable-length staff PINs), [#80](https://github.com/pmconnolly80/FinalCapstone/issues/80)
   (PIN-pad availability flag), [#81](https://github.com/pmconnolly80/FinalCapstone/issues/81)
   (customer-facing unavailable report) — all assigned to Milestone 8.
2. **The 5 previously-unassigned issues (#74–#78) got a real sprint**: Milestone 8,
   "Admin & Engagement UX Follow-ups," created and all 5 assigned — bundled by size
   (small, independent fixes) rather than by shared epic.
3. **Tracking drift fixed**: Milestone 4 ("Sprint 4: Auth II") was marked closed in
   GitHub while issues #40–#46 were still open. Verified each of the 6 closing PRs
   (#47–#52) was actually merged and explicitly referenced the right issue number in
   its body, then closed all 7 issues. Milestone 4 now correctly shows 0 open / 7
   closed.
