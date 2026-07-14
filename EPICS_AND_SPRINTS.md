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

## Epics

| Epic | Label | Status |
|---|---|---|
| Core Catalog (browse/detail/CRUD) | `epic:core-catalog` | ✅ Done — pre-dates formal sprint tracking |
| Auth & Roles | `epic:auth` | ✅ Done — merged to `master` via [PR #7](https://github.com/pmconnolly80/FinalCapstone/pull/7) |
| **Mug Club Progress & Bartender Confirmation** | `epic:mug-club` | 🔵 In progress — Sprint 1 open. The actual product driver (see `PROJECT_PLAN.md` §1) |
| Discovery & Engagement (search/filter/favorites) | `epic:discovery` | ⬜ Not started |
| Admin Experience (dashboard, user/role mgmt UI, confirmation audit) | `epic:admin` | ⬜ Not started |
| Deployment & Hardening (AWS, CI/CD) | `epic:deployment` | ⬜ Not started |
| Future Enhancements (reviews, images, Open Brewery DB) | `epic:future-enhancements` | ⬜ Backlog, unscheduled |

## Sprints

### Sprint 1: Mug Club Core — [milestone](https://github.com/pmconnolly80/FinalCapstone/milestone/1) (open)

The core mug-club loop end to end: a bartender can confirm a beer for a customer, and that
customer can see their progress. This alone delivers the primary MVP driver described in
`PROJECT_PLAN.md` and `FEATURE_MAP.md`.

1. [#2 Add Tavern and BeerConfirmation entities + migration](https://github.com/pmconnolly80/FinalCapstone/issues/2)
2. [#3 API: bartender confirm-beer-for-customer endpoint](https://github.com/pmconnolly80/FinalCapstone/issues/3)
3. [#4 API: customer mug-club progress endpoint](https://github.com/pmconnolly80/FinalCapstone/issues/4)
4. [#5 UI: customer "My Progress" screen](https://github.com/pmconnolly80/FinalCapstone/issues/5)
5. [#6 UI: bartender "Confirm Beer" screen](https://github.com/pmconnolly80/FinalCapstone/issues/6)

### Sprint 2: Mug Club Completion — planned, not yet ticketed

Finishes the epic once Sprint 1's core loop is verified working:

- "Mug earned" milestone flag/notification once a customer hits 200 confirmed beers
- Admin tooling to review and correct a bartender's confirmation history (mistakes happen at
  the bar; there needs to be a fix path)

### Later sprints (named only — groomed into issues when they're next up)

- **Discovery & Engagement** — search/filter/sort on the catalog, favorites
- **Admin Experience** — admin dashboard, beer management table polish, user/role management UI
  (role assignment is currently DB-manual only, see `CLAUDE.md`)
- **Deployment & Hardening** — AWS deployment per `beer-app/infra/aws-architecture.md`, CI/CD,
  mobile polish pass
- **Future Enhancements** — reviews/ratings, images, Open Brewery DB integration, recommendations

## Session traceability

Every working session gets an entry in [`SESSION_LOG.md`](SESSION_LOG.md) noting which
sprint/story it touched. That, plus each issue/PR referencing the commits that closed it, is
how a reader can confirm the work actually stayed in line with this plan rather than drifting.
