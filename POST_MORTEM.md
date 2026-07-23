# Post-Mortem — Sprints 1 through 5

A retrospective on the FinalCapstone / Mug Club Tracker build so far: what shipped, what
worked, what broke and how it got caught, and what to carry forward. Written 2026-07-23,
right after Sprint 5 (Admin Experience) closed. See `EPICS_AND_SPRINTS.md` for the live
status board and `SESSION_LOG.md` for the full dated history this document summarizes.

## What shipped

| Sprint | Scope | Issues | PRs | Suite at close |
|---|---|---|---|---|
| Foundation (pre-Sprint-1) | Migrations, seed data, role-based auth | — | #7 | — |
| **1: Mug Club Core** | Bartender-PIN confirmation loop, progress screen | #2–#6 | #11 | — |
| **2: Mug Club Completion** | PIN lockout/lifecycle, durable mug award, admin void/audit + 2 interrupts (#17 registration bug, #18 landing page) | #12–#18 | #19–#25 | backend 85/85, frontend 61/61 |
| **3: Customer Phone Experience** | Availability states, search, OBDB brewery enrichment, Catalog.beer pre-fill, mobile repair | #26–#32 | #33–#39 | backend 131/131, frontend 99/99 |
| **4: Auth II** | Social sign-in (Google/Facebook/Apple), account linking, password reset, privacy/data-deletion | #40–#46 | #47–#52 | backend 171/171, frontend 117/117 |
| **5: Admin Experience** | AdminAudit trail, role assignment, user management, audited beer edit/delete, Beer Management Table, anomaly detection, Admin Dashboard | #53–#59 | #60–#66 | backend 236/236, frontend 149/149 |

Five sprints, four of them user-facing product epics now fully done: **Mug Club
Progress & Bartender Confirmation**, **Customer Phone Experience**, and **Admin
Experience** are all ✅ complete; **Auth & Roles** is done for password auth plus
social sign-in. Backend tests grew from 0 → 236, frontend from 0 → 149, every one of
them written before the code it tests per this project's Definition of Done — none
were backfilled after the fact.

Remaining: **Engagement, Retention & Social** (not yet groomed into issues) and
**Deployment & Hardening** (AWS/CI-CD, not yet started).

## What went well

**TDD held, all the way through.** Every story in `SESSION_LOG.md` reports tests
written first, then the implementation, then a live-verification pass — not
"implement, then backfill tests to get the number up." This is stated as policy in
`EPICS_AND_SPRINTS.md`'s Definition of Done and it actually happened, session after
session, without drift.

**Live verification caught things tests didn't, consistently.** Nearly every shipped
story includes a "Live-verified against Docker" step beyond the test suite, and it
regularly found real problems the automated tests missed:
- #26 (`Beer.Availability`): a plain `HttpClient` failed to parse the enum back from
  JSON because `JsonStringEnumConverter` was registered on `Program.cs`'s own
  `JsonOptions`, not caller-agnostically — only visible when hitting the real HTTP
  contract, not the in-process test host.
- #28 (search-first beer list): `docker compose up --build web` silently served a
  stale bundle because `docker-compose.yml` had no volume mount for `web` — a
  `verify`-skill doc claiming otherwise (carried over from an earlier setup) got fixed
  on the spot so it wouldn't cost a session again.
- #55 (User Management screen): manual DB-bootstrap testing produced a user with two
  role rows, which crashed `GetUsers` entirely (`ToDictionaryAsync` throwing on a
  duplicate key) — caught and fixed with a regression test, something no unit test
  had exercised because the app's own code never produces that state.

**A deliberate "second pass" habit paid for itself twice in Sprint 5.** Asked
explicitly to plan #58 and #59 "looking for issues that could arise" before
implementing surfaced real problems *before* any code was written: #58's plan review
caught a broken, unlabeled pseudocode sketch and an `int.Parse` inside an EF LINQ
predicate that would have thrown at runtime; #59's investigation surfaced that "active
members" had no fixed definition (this codebase's own `PERSONAS_AND_USAGE.md` defines
it differently than the cheap reading would) and that "becomes the landing page for
the Admin role" wasn't actually true yet. Both got resolved as explicit decisions with
the user rather than silently guessed.

**Doc-driven continuity actually worked across a long, multi-session project.**
`CLAUDE.md` (current state), `EPICS_AND_SPRINTS.md` (the board), and `SESSION_LOG.md`
(dated history) meant every new session — including ones with a fully reset context —
could pick up exactly where the last one left off without re-deriving status from the
code. The "only the next epic gets ticketed" grooming rule kept the backlog honest
instead of pre-planning sprints that were likely to change shape before they mattered.

**Bugs got a real process, not ad hoc fixing.** The `bug` label + interrupt convention
(established 2026-07-14 when live testing broke registration, #17) meant defects
against shipped work had a clear path: interrupt the current sprint if it blocks
things, otherwise queue for grooming — and every fix still shipped with a test that
would have caught it.

## What went wrong, and how it got caught

| Issue | Where | How it surfaced | Fix |
|---|---|---|---|
| Registration silently failed on non-compliant passwords | Sprint 2 interrupt (#17) | Live user testing of the running app | Explicit length-only password policy + surfaced API error messages, with a test asserting the exact message |
| `PostBeer` never recorded who created a beer | #58 investigation | Reviewing #58's own design against the architecture doc's stated purpose ("a burst suggests a compromised admin account") | Extended `PostBeer` to write an `AdminAudit` row — confirmed as an intentional scope addition with the user first |
| `GetUsers` 500s on a user with >1 role row | #55 manual verification | A DB-bootstrap step for testing produced exactly that state | `GroupBy`-then-`ToDictionary` instead of `ToDictionaryAsync`, plus a regression test |
| Broken pseudocode sketch left unlabeled in a plan | #58 planning review | Explicit "review this plan again, look for issues" pass before coding | Replaced with real, compiled-and-tested logic; added a `CLAUDE.md` "Planning conventions" section so this doesn't recur |
| `int.Parse` inside an EF Core LINQ predicate (would 500 at runtime) | Same #58 review | Same review pass | Rewritten as a plain string comparison, which EF can translate |
| "Active members" and "landing page" both had unstated definitions | #59 investigation | Cross-checking the issue text against `PERSONAS_AND_USAGE.md` and `Home.jsx`'s actual behavior | Both resolved as explicit either/or decisions with the user before design, not guessed |
| Stale Docker web bundle during Sprint 3 | #28 live verification | `docker compose up --build web` didn't pick up changes | Traced to a missing volume mount claim in the `verify` skill doc; corrected on the spot |

None of these were caught by the automated test suites alone — every one needed
either live verification against the real stack, or a deliberate second look at the
plan/code before or after it shipped. That's the throughline: **tests prove the logic
does what it's supposed to; live verification and review passes are what catch "what
it's supposed to do isn't fully defined yet" or "this only breaks against real
infrastructure."**

## Process lessons worth carrying forward

1. **Keep asking "what does this term actually mean" before building a metric.**
   "Active members," "landing page," and (earlier) the mug-club's own "earned is
   permanent" edge case all needed an explicit decision rather than an assumed
   reading. The cost of asking is one clarifying question; the cost of guessing wrong
   is a mislabeled dashboard number a tavern owner might actually rely on.
2. **A deliberate re-review pass before coding is cheap insurance on anything with
   real logic.** Both #58 and #59's "look for issues" requests found real problems
   that a straight plan-then-implement pass had missed. This is worth doing
   proactively on config-driven thresholds, date/time math, and anything touching
   money or trust (PIN confirmation, audit trails), not just when explicitly asked.
3. **Explicit `now`/clock parameters beat `DateTime.UtcNow` reads buried in logic.**
   Established in #58, reused in #59 — any code with time-window logic (buckets,
   "today," "last 30 days") should take the reference time as a parameter so tests are
   deterministic regardless of when they run. This should be the default going
   forward, not a special case.
4. **Live verification is not optional for anything touching HTTP contracts, Docker,
   or an external API.** Multiple real bugs (the enum-serialization wrinkle, the stale
   web bundle) were invisible to the in-process test host and only showed up against
   the actual running stack. Budget for it on every story that isn't pure business
   logic.
5. **Small, well-scoped gap-filling additions (extending #56's audit pattern to
   `PostBeer`, for instance) are worth doing when discovered, with the user's explicit
   sign-off** — better than shipping a feature that's technically in-scope but
   practically useless (an anomaly that can't say who caused it).

## Open risks / known gaps

- **Engagement, Retention & Social** is entirely unbuilt — badges, push notifications,
  My Beers, social feed, and owner analytics are all still doc-only. This is the
  actual business-owner payoff per `PERSONAS_AND_USAGE.md`, so it's the natural next
  sprint to groom.
- **Deployment & Hardening** hasn't started — the app has never run outside a local
  Docker Compose stack. `infra/aws-architecture.md` is a design doc only, no IaC yet.
- The live-tested external integrations (Open Brewery DB, Catalog.beer, Google/
  Facebook/Apple OAuth) have real test coverage for their business logic, but several
  of the OAuth challenge/callback round trips against a *live* provider app are still
  flagged in `CLAUDE.md` as unverified in this environment — worth a manual pass
  before real users hit them.
- No admin UI exists yet for anything in the Engagement/Retention epic, and
  Deployment & Hardening's security backlog items from the original code audit
  (JWT signing key defaults, CORS policy) haven't been revisited since they were
  first flagged.

## Bottom line

Five sprints, ~60 issues, ~30 merged PRs, zero backfilled tests, and a doc set that
kept every session oriented without re-deriving status from scratch. The main failure
mode wasn't broken logic — the test suites caught that reliably — it was **unstated
assumptions** (what "active" means, what "landing page" means, who gets attributed for
an action) and **infrastructure-only bugs** (JSON serialization across process
boundaries, stale Docker volumes). Both are addressed the same way: ask before
guessing, and verify against the real stack before calling something done.
