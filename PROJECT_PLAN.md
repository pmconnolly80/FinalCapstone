# BeerList Refactor Plan

## 1. Project goal

Refactor the existing ASP.NET MVC beer catalog into a modern product with:
- a React frontend
- an open-source backend API
- a clearer domain model
- room for future features such as search, reviews, ratings, user profiles, and admin tools

The initial goal is not to rebuild everything at once. The goal is to preserve the current value of the app while moving to a more scalable architecture.

### Real-world driver: the tavern's mug club

The concrete use case behind this app: a local tavern runs a "200 club" — drink 200 different beers off their list and earn a mug. Today this is tracked on a printed sheet where the bartender initials next to each beer as a customer drinks it. This app replaces that paper process:
- each customer has a digital progress list (which of the ~200 beers they've had)
- a bartender confirms a beer at the point of service, which is what actually marks it complete on the customer's list (customers can't self-report — it must be bartender-confirmed, same as the paper initials)
- the customer can see their own progress toward 200
- the data model should anticipate more than one tavern/location down the line, even though the first build targets a single tavern's list

This is the primary MVP driver, not a secondary feature — search/filter/CRUD on the beer catalog exists to support this, not the other way around.

## 2. Recommended target architecture

### Frontend
- React with Vite or Next.js
- TypeScript
- React Router
- TanStack Query or Redux Toolkit for data fetching/state
- Tailwind CSS or a simple component system

### Backend
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL or SQL Server
- Open-source authentication using ASP.NET Core Identity or a simpler auth strategy

### Why this stack
- It stays close to the current C#/.NET ecosystem while modernizing the stack.
- It is fully open-source.
- It supports a clean split between frontend and backend.
- It is a good fit for future growth.

## 3. Product vision

The app should evolve from a paper mug-club sheet into a digital progress tracker that
lives on the customer's phone. The defining moment: a customer orders a beer, searches for
it in the app, reads about it (including real brewery info from Open Brewery DB), and gets
it bartender-confirmed toward their 200.

- search-first catalog: find the beer you're drinking in a few keystrokes (name, brewery,
  style; filter by had / not had yet)
- beer detail pages worth reading — the tavern's style/description plus brewery info
  (location, type, website) enriched from Open Brewery DB (note: OBDB is a *brewery*
  directory only; the tavern's own list stays the source of truth for the beers themselves)
- a bartender-confirmation workflow that marks a beer complete on a customer's list —
  including an "I'm drinking this" request the customer initiates and the bartender
  approves with one tap
- per-customer progress toward the 200-beer goal, with milestone badges along the way and
  a clear "mug earned" moment at the end
- user accounts (customer and bartender/staff roles)
- retention features that make the app pay off for the bar owner: notifications (new
  beers, progress nudges, win-back), seasonal mini-challenges, opt-in leaderboard, a
  personal beer journal (favorites, tasting notes)
- owner analytics: which beers get confirmed most/least (purchasing signal), member
  activity and lapsed members (promotion signal)
- admin moderation tools
- support for more than one tavern/location eventually

## 4. Phase-based roadmap

### Phase 0 — Discovery and planning
Duration: 1 week

Goals:
- confirm the MVP scope
- define user roles
- define the beer data model more clearly
- decide on the target backend stack
- outline the first release backlog

Deliverables:
- finalized product requirements
- user stories
- initial UX flow
- repo structure decision

### Phase 1 — Foundation setup
Duration: 1 week

Goals:
- create the new frontend and backend projects
- establish CI/CD basics
- define the shared API contract
- set up local development environment

Deliverables:
- frontend app scaffold
- backend API scaffold
- basic local config
- deployment pipeline skeleton

### Phase 2 — MVP migration
Duration: 2 weeks

Goals:
- recreate the current beer catalog experience in the new stack
- move beer CRUD to the new API
- connect React UI to the API
- preserve the current core functionality

Deliverables:
- list beers
- beer details
- create/edit/delete beers
- authentication and protected actions
- basic styling and layout

### Phase 3 — Product expansion
Duration: 2–4 weeks

Goals:
- add search and filtering
- add user favorites
- add ratings/reviews
- improve admin workflows
- improve error handling and validation

Deliverables:
- richer catalog experience
- user-aware features
- admin tooling

### Phase 4 — Hardening and release readiness
Duration: 1 week

Goals:
- test coverage
- performance tuning
- deployment readiness
- docs and onboarding

Deliverables:
- production-ready setup
- environment config
- operational documentation

## 5. MVP scope for the first release

The first release should focus on the current app’s value, not the future vision.

### Must-have features
- browse beer catalog
- view beer details
- create, update, and delete beers
- user registration and login
- role-based editing permissions (admin, bartender, customer)
- bartender flow to confirm a beer against a specific customer's list
- customer view of their own progress toward 200 beers, with a "mug earned" milestone
- responsive UI
- API-backed data storage

### Nice-to-have later
- search by name/style/brewery
- favorites and watchlists
- ratings and comments
- advanced admin dashboard
- import/export data
- Open Brewery DB API integration (https://www.openbrewerydb.org/) — scoped July 2026: the
  API provides brewery data only (no beer-level endpoint), so it's used to enrich beer
  detail pages with brewery info and to power brewery autocomplete in the admin form; the
  tavern's list remains the source of truth for the beers

## 6. Recommended backlog

### Epic 1 — Core catalog
- As a visitor, I can view beers
- As a visitor, I can view details of a beer
- As an editor, I can create a beer
- As an editor, I can edit a beer
- As an editor, I can delete a beer

### Epic 2 — Authentication
- As a user, I can register an account
- As a user, I can log in and log out
- As an admin, I can manage permissions

### Epic 2.5 — Mug club progress and bartender confirmation
- As a customer, I can see which beers on the list I've had and how many I have left until 200
- As a bartender, I can look up a customer and confirm they drank a specific beer, marking it complete on their list
- As a customer, I am notified or shown a milestone when I hit 200 beers and earn a mug
- As an admin, I can see and correct confirmation history if a bartender makes a mistake

### Epic 3 — Discovery and engagement
- As a user, I can search beers
- As a user, I can filter by style or brewery
- As a user, I can save favorite beers

### Epic 4 — Admin experience
- As an admin, I can review and moderate content
- As an admin, I can manage users and roles

## 7. Data migration approach

The current app uses a LocalDB-backed Entity Framework model. The migration should be done carefully.

Recommended steps:
1. export the existing beer data
2. create the new database schema in PostgreSQL or SQL Server
3. map old fields to the new model
4. import existing records
5. verify the migrated data with sample queries

## 8. Risks and mitigations

### Risk: overbuilding too early
Mitigation: start with a clean MVP and defer advanced features.

### Risk: too much rewrite at once
Mitigation: keep the current domain model simple and migrate in stages.

### Risk: authentication complexity
Mitigation: start with a straightforward auth flow and grow it later if needed.

### Risk: unclear product direction
Mitigation: define the first release around the beer catalog and keep future features in a backlog.

## 9. Suggested next steps

1. confirm the MVP scope with a short product checklist
2. choose the backend database and hosting target
3. scaffold the React frontend and ASP.NET Core API
4. build the beer list and detail flow first
5. add authentication and edit permissions next

## 10. Suggested milestone plan

### Milestone 1 — Working MVP
- React app renders beer catalog data from the API
- CRUD works for beers
- users can authenticate

### Milestone 2 — Feature-rich catalog
- search, filters, favorites, and improved admin workflows

### Milestone 3 — Production launch
- deployment, monitoring, and documentation

---

If you want, the next step should be a structured planning session where we define:
- the exact MVP feature set
- the initial database schema
- the first user stories
- the repo layout for the new frontend/backend split
