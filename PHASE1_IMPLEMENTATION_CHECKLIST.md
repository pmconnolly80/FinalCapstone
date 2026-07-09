# Phase 1 Implementation Checklist

## 1. Define the initial domain model

- [ ] Confirm the first beer fields needed for MVP
- [ ] Decide whether description, image, and tags are in v1 or later
- [ ] Define the initial user role model
- [ ] Decide how admin and editor permissions will work

## 2. Set up the backend API

- [ ] Create a new ASP.NET Core Web API project
- [ ] Add Entity Framework Core and PostgreSQL support
- [ ] Create a Beer entity and DbContext
- [ ] Add initial migration
- [ ] Create endpoints for:
  - [ ] GET /beers
  - [ ] GET /beers/{id}
  - [ ] POST /beers
  - [ ] PUT /beers/{id}
  - [ ] DELETE /beers/{id}

## 3. Set up the frontend app

- [ ] Create a React or Next.js project
- [ ] Add TypeScript
- [ ] Set up a basic app shell
- [ ] Add routing for:
  - [ ] Home
  - [ ] Beer list
  - [ ] Beer detail
  - [ ] Create/edit beer
  - [ ] Login/register

## 4. Connect frontend to backend

- [ ] Add API client configuration
- [ ] Fetch and render the beer list
- [ ] Build the beer detail view
- [ ] Submit create/edit forms
- [ ] Handle loading, error, and empty states

## 5. Add authentication

- [ ] Set up authentication flow
- [ ] Add login and registration UI
- [ ] Protect admin-only actions
- [ ] Add role-based authorization rules

## 6. Make it mobile-first

- [ ] Design the main screens for phone screens first
- [ ] Ensure touch-friendly buttons and forms
- [ ] Optimize spacing and navigation for small screens
- [ ] Test main flows on a mobile viewport

## 7. Prepare deployment

- [ ] Create a local environment configuration
- [ ] Set up a development database
- [ ] Prepare AWS hosting plan
- [ ] Add basic CI/CD workflow if desired

## 8. First milestone definition

The first milestone is complete when:
- [ ] the beer list loads from the API
- [ ] users can view beer details
- [ ] authorized users can create/edit/delete beers
- [ ] login and auth work
- [ ] the experience works on a phone screen
