# Implementation Backlog

## Phase 1 — Foundation

### Project setup
- Create frontend project structure
- Create backend API project structure
- Set up shared configuration
- Set up local development environment

### Data model
- Define Beer entity
- Define User and role model (customer, bartender, admin)
- Define BeerConfirmation entity (customer, beer, confirming bartender, timestamp) — this is what marks a beer complete on a customer's list
- Define Tavern/Location entity now so the model isn't single-tenant-locked, even though v1 only has one tavern's data
- Create initial database schema
- Add migration strategy

## Phase 2 — Core catalog MVP

### Beer list
- Create API endpoint to list beers
- Create UI list screen
- Add loading and empty states

### Beer details
- Create API endpoint to fetch one beer
- Create detail screen
- Add error handling

### Create/edit/delete
- Create API endpoints for CRUD
- Build forms in UI
- Add validation and success states

## Phase 3 — Authentication and authorization

- Add login and registration flow
- Add protected routes
- Add role-based permissions for editing (admin, bartender, customer)
- Add session handling

## Phase 3.5 — Mug club progress and bartender confirmation

- Build customer lookup (by name search, with QR/membership-code lookup as a stretch goal)
- Build bartender "confirm beer for customer" action
- Build customer progress view (X of 200, list of remaining beers)
- Add "mug earned" milestone at 200 confirmed beers
- Add admin tooling to review/correct confirmations

## Phase 4 — Customer phone experience (search-first redesign)

The app lives on the customer's phone; this phase makes that true rather than aspirational.

### Search as the front door
- Search endpoint on the API (name/brewery/style, paginated) — the current GET-all won't
  scale to a 200-beer list on a phone
- Search bar with autocomplete on the beer list, filter chips for style/brewery and
  had / not had yet
- Per-beer confirmed checkmark in list results for the signed-in customer

### Open Brewery DB enrichment (breweries only — the API has no beer-level data)
- Store an Open Brewery DB brewery id on each beer
- Beer detail shows brewery card (type, city/state, website) from cached OBDB data
- Brewery autocomplete against OBDB in the admin add/edit-beer form
- Server-side caching so the tavern's app doesn't hammer or depend on OBDB uptime

### Mobile UX repair
- Home screen = customer progress + search, not a generic catalog welcome
- Auth-aware navigation (logout, logged-in state; hide Add Beer from customers)
- Remove beer CRUD from the customer surface (admin-only)
- Fix API base URL config so a real phone can reach the API (no hardcoded localhost)
- Optimize layout for phone screens, responsive navigation
- Improve form usability (input types, labels, validation)
- Improve loading and error states (errors currently only go to the console)

## Phase 5 — Admin experience

- Build admin dashboard
- Add beer management table (catalog CRUD's new — and only — home)
- Add user management tools (role assignment is currently DB-manual)
- Add moderation workflow basics

## Phase 6 — Engagement & retention (the business-owner payoff)

- "I'm drinking this" confirmation-request queue (customer initiates, bartender one-tap
  approves)
- QR membership code for instant customer lookup
- Milestone badges at 25/50/100/150 plus the "mug earned" moment
- Notifications: new beers on the list, "N to go" nudges, win-back after inactivity
- Seasonal mini-challenges and opt-in leaderboard
- Personal beer journal: favorites, tasting notes, private ratings
- Owner analytics: most/least confirmed beers, member activity, lapsed-member list

## Phase 7 — Future enhancements

- Public reviews and ratings
- Images
- Recommendations
- Full metrics and reporting
