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
- beer detail pages worth reading — the data beer nerds love: style and style family,
  ABV, IBU, description, plus brewery info (location, type, website) enriched from Open
  Brewery DB. Sourcing principle (July 2026): auto-enrich from open projects first
  (OBDB for breweries; Catalog.beer is the researched candidate for beer-level fields)
  so staff never *have* to type beer data — manual entry stays available as fallback and
  override, and the tavern's own list stays the source of truth
- a bartender-confirmation workflow that lives entirely **on the customer's phone**
  (decided July 2026 — no bar tablet, no bartender device): the customer finds the beer,
  taps "Confirm with bartender," and hands their phone over; the bartender types their
  **personal 6-digit PIN**, which authorizes and attributes the confirmation — the
  digital version of initialing the customer's paper sheet
- per-customer progress toward the 200-beer goal, with milestone badges along the way and
  a clear "mug earned" moment at the end
- a catalog that handles the bar's reality — a large, constantly rotating inventory:
  every beer has an availability state (on tap / out of stock / retired), search defaults
  to what's in stock tonight, and confirmations are permanent even when beers rotate off
- user accounts (customer and bartender/staff roles), with **social sign-in** — Google,
  Facebook, or Apple alongside email/password — so joining the club is one tap on a
  barstool, and the bar gains a verified contact (consent-gated) for targeted marketing
- retention features that make the app pay off for the bar owner: **push notifications**
  (owner-composed announcements with audience targeting, plus automated new-beer,
  progress-nudge, and win-back sends — the frontend becomes an installable PWA),
  seasonal mini-challenges, a personal beer journal (tasting notes)
- "My Beers" for the customer (added July 2026): the completed list (every confirmed
  beer with its date), personal 1–5 star ratings on beers they've had (prompted right
  after confirmation), a **want list** to work from when they're not sure what to order
  (filtered to what's in stock tonight, auto-checked-off on confirmation, with an
  automated push when a wanted beer comes on tap), and **My Stats** — beer-nerd
  visualizations of completions and ratings (styles explored, ABV spread, progress over
  time, what they rate highest)
- a social layer among the bar's members — opt-in activity feed of milestones, cheers,
  leaderboard, communal goal widget, wall of mugs — so the club feels like a community
  working the same list, not 200 solo climbs
- owner analytics: which beers get confirmed most/least (purchasing signal), member
  activity and lapsed members (promotion signal)
- admin moderation tools, including full data correction: an admin can edit any record —
  beers, confirmations, accounts, social content — to fix inaccuracies or questionable
  submissions, with every correction audited (who, when, why), and automatic anomaly
  alerts to the owner and admin when something abnormal happens (e.g. an unusual burst of
  beers added to the catalog at once)
- support for more than one tavern/location eventually

Persona-level detail on how each of these plays out at the bar — for customer, bartender,
owner, and admin — lives in `PERSONAS_AND_USAGE.md`.

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
- add the want list and personal ratings (My Beers)
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
- bartender PIN entry on the customer's phone (design the confirm endpoint to carry the
  `pin` field from day one; ship PIN validation right after the core loop works)
- beer availability states for the rotating inventory (in-stock-by-default search)
- push notifications (installable PWA + web push; owner composer with audience targeting
  and automated nudges)
- social layer v1 (opt-in display name, milestone activity feed, cheers, leaderboard,
  communal goal, wall of mugs)
- social sign-in (Google/Facebook/Apple via ASP.NET Core Identity external providers —
  researched July 2026, see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6) with marketing-consent
  capture
- want list (supersedes the earlier favorites/watchlists idea) with in-stock filter and
  on-tap push trigger
- personal 1–5 ratings on confirmed beers + My Stats visualizations
- public ratings and comments
- advanced admin dashboard
- import/export data
- Open Brewery DB API integration (https://www.openbrewerydb.org/) — scoped July 2026: the
  API provides brewery data only (no beer-level endpoint), so it's used to enrich beer
  detail pages with brewery info and to power brewery autocomplete in the admin form —
  the admin stops hand-typing brewery data and the customer gets real info about where
  the beer comes from; the tavern's list remains the source of truth for the beers

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
- As a user, I can sign in with Google, Facebook, or Apple instead of creating a password, and link more than one provider to the same account so my progress never forks
- As a user, I can opt in (or not) to marketing communications at signup, and my choice is stored
- As an owner, I get a verified email and name for each consenting member, feeding targeted marketing segments
- As an admin, I can manage permissions

### Epic 2.5 — Mug club progress and bartender confirmation
- As a customer, I can see which beers on the list I've had and how many I have left until 200
- As a customer, I can tap "Confirm with bartender" on the beer I'm drinking and hand my phone across the bar
- As a bartender, I type my personal 6-digit PIN on the customer's phone to confirm the beer — my whole interaction with the app, recorded under my name like initials on the paper sheet
- As a customer, I am notified or shown a milestone when I hit 200 beers and earn a mug
- As an admin, I can see and correct confirmation history if a bartender makes a mistake

### Epic 3 — Discovery and engagement
- As a user, I can search beers, seeing what's in stock tonight by default
- As a user, I can filter by style or brewery, and by had / not had yet
- As a user, I can save favorite beers

### Epic 3.5 — Push notifications and social
- As a customer, I can install the app to my phone's home screen and opt into push notifications
- As an owner, I can compose and send a push notification to a targeted audience (all / active / lapsed / hasn't-had-beer-X)
- As a customer, I automatically hear about new beers, my next milestone, and get a win-back nudge if I've gone quiet
- As a customer, I can opt into the social layer with a display name and see a feed of member milestones, cheer on other members, and check the leaderboard
- As an owner, I can show a communal goal ("the bar has drunk N club beers this year") and a wall of mug earners

### Epic 3.6 — My Beers: ratings, want list, personal stats (added July 2026)
- As a customer, I can see the full list of beers I've completed, with confirmation dates, sortable by date, name, style, or my rating
- As a customer, I can rate any beer I've had confirmed 1–5 stars — asked "How was it?" right on the confirmation success screen, and editable later
- As a customer, I can add beers to a want list from search or a beer's page, and open it filtered to what's in stock tonight when I'm not sure what to order
- As a customer, a confirmed beer is automatically checked off my want list, and I get a push when a beer I want comes on tap
- As a beer nerd, I can see visualizations of my completions and ratings: progress over time, styles explored vs. remaining, ABV spread, what I rate highest
- As an owner, I see want-list demand counts and anonymized average ratings per beer as purchasing signals

### Epic 4 — Admin experience
- As an admin, I can review and moderate content (including social display names and feed items)
- As an admin, I can manage users and roles
- As an admin, I can issue, reset, and deactivate bartender PINs
- As an admin, I can keep the catalog current as inventory rotates (availability states, OBDB brewery autocomplete on add/edit)
- As an admin, I can edit any data in the system — beers, confirmations, accounts, social content — to correct inaccuracies or questionable submissions, with every change audited (who, when, why)
- As an owner or admin, I am automatically notified when abnormal activity occurs, such as an unusual burst of beers added to the catalog at once or a confirmation velocity spike

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
- search, filters, want list/ratings, and improved admin workflows

### Milestone 3 — Production launch
- deployment, monitoring, and documentation

---

If you want, the next step should be a structured planning session where we define:
- the exact MVP feature set
- the initial database schema
- the first user stories
- the repo layout for the new frontend/backend split
