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
