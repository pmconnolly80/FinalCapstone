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
- Social sign-in (July 2026, researched — see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6):
  Google + Facebook + Apple via ASP.NET Core Identity external login providers, linking
  to the existing Identity user on verified email; the API keeps issuing its own JWT
- Account-linking UI (multiple providers → one member, progress never forks)
- Marketing-consent checkbox at signup, stored per member; privacy policy page and
  data-deletion path (required for Facebook app review)
- Password reset (added 2026-07-14): "forgot password" flow using Identity's built-in
  reset tokens (`GeneratePasswordResetTokenAsync`/`ResetPasswordAsync`); needs an email
  sender (SMTP/SES) the app doesn't have yet — first email-delivery dependency, shared
  with the later push/notification work. Reset form enforces the same length-only min-8
  policy as registration

## Phase 3.5 — Mug club progress and bartender confirmation

One-device rule (decided July 2026): the whole flow is on the customer's phone — no
bartender device, no lookup step, no request queue.

- Build the confirm endpoint: `POST /api/confirmations {beerId, pin}` authenticated as
  the customer; carry the `pin` field from day one
- Build the Confirmation PIN Pad screen (full-screen takeover from beer detail: beer +
  customer name large, masked 6-digit pad, success state with updated count)
- `StaffPin` entity + server-side PIN validation (hashed, unique among active staff),
  two-axis lockout (per-PIN and per-customer-account)
- Build customer progress view (X of 200, list of remaining beers)
- Add "mug earned" milestone at 200 confirmed beers
- Add admin tooling to review/correct confirmations
- PIN lifecycle in user management: issue, reset, deactivate; staff change their own

## Phase 4 — Customer phone experience (search-first redesign)

The app lives on the customer's phone; this phase makes that true rather than aspirational.

### Search as the front door
- Search endpoint on the API (name/brewery/style, paginated) — the current GET-all won't
  scale to a rotating catalog that outgrows 200 rows on a phone
- `Beer.Availability` (on tap / available / out of stock / retired); search and browse
  default to in-stock, since the bar's inventory changes constantly
- Search bar with autocomplete on the beer list, filter chips for style/brewery,
  availability, and had / not had yet
- Per-beer confirmed checkmark and availability badge in list results for the signed-in
  customer
- Confirmations stay permanent when beers go out of stock or retire (progress never goes
  backwards on inventory changes)

### Open Brewery DB enrichment (breweries only — the API has no beer-level data)
- Store an Open Brewery DB brewery id on each beer
- Beer detail shows brewery card (type, city/state, website) from cached OBDB data
- Brewery autocomplete against OBDB in the admin add/edit-beer form
- Server-side caching so the tavern's app doesn't hammer or depend on OBDB uptime

### Catalog.beer beer-level pre-fill (candidate — spike first)
- Spike: search a sample of the tavern's actual list against catalog.beer's API and
  measure the hit rate; go/no-go on the integration based on the result
- If go: on admin add-beer, search catalog.beer by name and pre-fill style/ABV/IBU/
  description for the admin to verify (tavern's list stays the source of truth; prefer
  `cb_verified` results); reuse the OBDB server-side caching service (1,000 req/month
  key limit); add the required CC BY attribution line where its data appears

### Mobile UX repair
- Home screen = customer progress + search, not a generic catalog welcome
- Auth-aware navigation (logout, logged-in state; hide Add Beer from customers)
- Remove beer CRUD from the customer surface (admin-only)
- Fix API base URL config so a real phone can reach the API (no hardcoded localhost)
- Optimize layout for phone screens, responsive navigation
- Improve form usability (input types, labels, validation)
- Improve loading and error states (errors currently only go to the console)

## Phase 5 — Admin experience

- Build admin dashboard (including the anomaly panel: bulk beer-add alerts, confirmation
  velocity spikes, off-hours activity)
- Add beer management table (catalog CRUD's new — and only — home) with inline
  availability editing for the rotating inventory
- Add user management tools (role assignment is currently DB-manual) including bartender
  PIN issue/reset/deactivate
- Full data correction: admin can edit any record — beers, confirmations, accounts,
  social content — with a required reason note and an audit log (who/what/when/why);
  nothing silently deleted
- Catalog write guardrail: an unusually large batch of beers added in a short window
  fires an automatic notification to owner and admin (informational, not blocking)
- Add moderation workflow basics (social display names, feed items)

## Phase 6 — Engagement, retention & social (the business-owner payoff)

- Milestone badges at 25/50/100/150 plus the "mug earned" moment
- Push notification infrastructure: PWA manifest + service worker, `PushSubscription`
  entity, VAPID keys in environment config, background delivery job with frequency caps
- Automated notifications: new beers on the list (batched), "N to go" nudges, win-back
  after inactivity
- Owner notification composer: audience targeting (all / active / lapsed /
  hasn't-had-beer-X, consent-gated), send/schedule, delivery counts
- Social layer v1 (opt-in, display name, default private): milestone activity feed
  (system-generated, no free-text posts), one-tap cheers, leaderboard, communal goal
  widget, wall of mugs; admin moderation hooks
- Seasonal mini-challenges
- Personal beer journal: tasting notes (ratings and favorites are covered by My Beers
  and the want list below)

### My Beers: ratings, want list, personal stats (added July 2026)
- `BeerRating` entity + endpoints (1–5, unique per user+beer, requires an existing
  confirmation; private by default)
- `WantListItem` entity + endpoints (unique per user+beer; auto-resolved when the beer
  gets confirmed)
- "How was it?" rating prompt on the PIN pad success state (skippable, editable later)
- My Beers screen: completed list with dates + ratings, search, sort by date/name/style/
  rating
- Want List screen: add from search/detail, in-stock-tonight filter on by default,
  auto-check-off moment
- On-tap trigger: availability flip to on-tap → targeted push to wanting members via the
  Phase 6 push pipeline (frequency-capped)
- `GET /api/me/stats` aggregate endpoint (progress over time, style-family breakdown,
  ABV distribution, rating distribution + avg by style, explored-vs-remaining by style)
- My Stats screen with lightweight client-side charts over that payload
- Owner dashboard aggregates: anonymized avg rating per beer, want-count per beer

- Owner analytics: most/least confirmed beers, member activity, lapsed-member list,
  consent-gated marketing segments (sign-in identity + in-app behavior)

### External beer search + customer recommendations (groomed 2026-07-23 as Sprint 7)
Flagged during 2026-07-23 live testing, now groomed into
[#72](https://github.com/pmconnolly80/FinalCapstone/issues/72)/[#73](https://github.com/pmconnolly80/FinalCapstone/issues/73)
(milestone [7](https://github.com/pmconnolly80/FinalCapstone/milestone/7)):
- Customer-facing search mode against the external catalogs already integrated on the
  admin side (`IBreweryLookupService`/Open Brewery DB, `ICatalogBeerService`/
  Catalog.beer) — clearly separated in the UI from search of the tavern's own list, so
  customers aren't confused about what the tavern actually serves
  (`CLAUDE.md`'s core framing: the mug club tracks the tavern's own list, not a
  generic catalog)
- Log what customers search for via that mode as an ordering-decision signal for the
  owner (which beers/breweries get searched but aren't on the tavern's list)
- `BeerRecommendation`/`BeerRequest`-style entity: customer suggests a beer (optionally
  sourced straight from an external search hit) for the tavern to stock
- Admin-facing review/triage screen for incoming recommendations — needed before this
  is actionable, not just a write-only inbox
- Natural pairing with the existing want-list demand-aggregation idea above (same
  "what do members want" signal, extended to beers not yet on the list at all)

## Phase 7 — Future enhancements

- Public reviews and ratings
- Images
- Recommendations
- Full metrics and reporting
