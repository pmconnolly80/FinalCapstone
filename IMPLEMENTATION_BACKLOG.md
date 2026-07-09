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

## Phase 4 — Mobile-first polish

- Optimize layout for phone screens
- Improve form usability
- Add responsive navigation
- Improve loading and error states

## Phase 5 — Admin experience

- Build admin dashboard
- Add beer management table
- Add user management tools
- Add moderation workflow basics

## Phase 6 — Future enhancements

- Search and filtering
- Favorites
- Reviews
- Images
- Metrics and reporting
- Integrate Open Brewery DB API (https://www.openbrewerydb.org/) to pull real brewery info and images into the catalog — revisit scope/design during next project planning pass
