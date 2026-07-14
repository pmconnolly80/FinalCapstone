# Mobile-First Product Outline

## 1. Product goal

Build a mobile-first beer application that is easy to use on a phone, while also supporting management workflows on a laptop.

Concretely: digitize a tavern's paper mug-club sheet, where a bartender initials next to a beer once a customer has had it. **The one-device rule (decided July 2026): the whole at-the-bar flow lives on the customer's phone** — the customer finds the beer, and the bartender's "initials" are a personal 6-digit PIN typed on the customer's phone. There is no bar tablet and no bartender screen; back-office management stays on a laptop.

## 2. Core usage pattern

The moment that defines the product happens at the bar: a customer orders a beer, pulls out
their phone, **searches** for that beer on the tavern's list, reads about it, and gets it
counted toward their 200. Everything else supports that moment.

- Primary customer interaction happens on a phone (search, beer info, viewing progress)
- Search is the customer's front door — finding the beer they're drinking in a few
  keystrokes, not scrolling a 200-item list
- Beer details are worth reading — the data beer nerds love: style (and family/class),
  ABV, IBU, description, plus real brewery info (location, website) pulled from Open
  Brewery DB. Principle: auto-source these fields from open projects so staff never have
  to type them (manual entry remains the fallback/override) — see
  `TECHNICAL_ARCHITECTURE_PLAN.md` §6
- Confirmation stays bartender-gated, but the whole flow runs on the customer's phone
  (decided July 2026): the customer taps "Confirm with bartender," a full-screen PIN pad
  appears showing the beer and customer name, and the phone is handed across the bar
- The bartender types their **personal 6-digit PIN** on the customer's phone — that's the
  bartender's entire interaction with the app. No bartender device, no queue to watch, no
  login mid-shift; the PIN authorizes the confirmation and attributes it by name, same
  meaning as initials on the paper sheet. Under five seconds, wet hands and all
- PINs are protected for the bartender's sake (they're typed on an untrusted device):
  masked entry, server-side validation only, two-axis lockout, and velocity/anomaly flags
  on the owner dashboard — see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1
- Search results default to what's **in stock tonight**: the inventory rotates constantly
  and the catalog will grow well past 200 entries, so availability (on tap / out of
  stock / retired) is first-class data and the default filter
- When the customer *isn't* sure what to order, the **want list** is the answer (added
  July 2026): beers they saved from earlier browsing, filtered to what's in stock tonight
  — a personal menu to work from. Right after a confirmation, "How was it?" captures a
  1–5 rating in one tap
- Back-office management and administration happen on a laptop
- The product should feel fast and lightweight on mobile
- The same core data and business rules should support both experiences

### Open Brewery DB — what it does and doesn't provide (verified July 2026)

Open Brewery DB's API (`api.openbrewerydb.org/v1/breweries`) is a **brewery** directory:
name, brewery type, address, coordinates, phone, website. It has **no beer-level data** —
no endpoint for individual beers, styles, or ABV. So:
- The tavern's own list stays the source of truth for the ~200 beers (true to the product
  anyway — the club is about *this tavern's* list).
- Each beer record links to an Open Brewery DB brewery id; the detail page shows live/cached
  brewery info (where it's from, website, brewery type).
- When an admin adds a beer, brewery autocomplete searches Open Brewery DB so brewery data
  is real and consistent instead of free-typed — with a large rotating inventory, cutting
  the admin's per-beer data entry is the difference between the catalog staying current
  and going stale. Beer-level fields (style, description) still come from the admin;
  OBDB makes the brewery half free.

## 3. Primary user experience on mobile

### Goals
- Quick access to core content
- Minimal friction for browsing and interacting
- Touch-friendly layouts
- Fast loading and simple navigation

### Mobile-first screens
- Home = my progress: the X-of-200 count is the first thing a signed-in customer sees,
  with the search bar directly beneath it
- Search and filter (name/brewery/style; filter by had / not had yet)
- Beer detail page — style, description, brewery info from Open Brewery DB, and a
  "Confirm with bartender" action that opens the full-screen PIN pad for the phone handoff
- Browse beer catalog
- My mug club progress (X of 200, remaining beers, milestone badges)
- My Beers — the completed list with dates and my 1–5 ratings, sortable (added July 2026)
- Want List — saved beers with in-stock-tonight filter, auto-check-off on confirmation
  (added July 2026)
- My Stats — beer-nerd visualizations of completions and ratings (added July 2026)
- Account / profile
- Confirmation PIN pad (on the customer's phone — beer and customer name shown large so
  the bartender can verify at a glance before keying their PIN; there are no
  bartender-facing screens beyond this shared moment)
- Later: social feed of member milestones, cheers, leaderboard (opt-in; see
  `PERSONAS_AND_USAGE.md`)

Deliberately removed from the customer's mobile surface: beer create/edit/delete. Catalog
management is an admin task and shouldn't sit in the customer's navigation.

## 4. Admin experience on laptop

### Goals
- Efficient management of catalog data
- Simple moderation and oversight tools
- Clear views for users, roles, and content

### Laptop-first screens
- Admin dashboard
- Beer management table (with availability states for the rotating inventory and OBDB
  brewery autocomplete)
- Create / edit / delete workflows
- User management (roles + bartender PIN issue/reset/deactivate)
- Data correction: admin can edit any record — beers, confirmations, accounts, social
  content — to fix inaccuracies or questionable submissions, every change audited
- Owner: push-notification composer with audience targeting (all / active / lapsed /
  hasn't-had-beer-X), send/schedule, basic delivery results
- Owner/admin anomaly panel: bulk beer-add alerts, confirmation velocity spikes,
  off-hours activity
- Analytics / reporting views

## 5. Experience split

| Experience | Primary device | Main goal |
|---|---|---|
| Consumer browsing | Phone | Discover and interact quickly |
| Content management | Laptop | Manage data efficiently |
| Account and settings | Phone or laptop | Maintain profile and preferences |

## 6. Design principles

- Mobile-first layout
- Responsive UI across phone and laptop
- Fast interactions
- Clear information hierarchy
- Low cognitive load
- Accessible and touch-friendly controls

## 7. MVP scope for mobile-first version

### Must-have mobile flows
- Search beers (the primary entry point at the bar)
- View beer details, including Open Brewery DB brewery info
- Browse beers
- Log in / sign up — social sign-in first (Google/Facebook/Apple, one tap at the bar;
  marketing-consent checkbox captured at signup), email/password as fallback
- Customer: view mug club progress toward 200
- Confirmation: "Confirm with bartender" PIN pad on the customer's phone (the bartender
  types their PIN — no bartender-side flow exists)

### Must-have laptop flows
- Admin dashboard
- Manage beer records (create/edit/delete lives here, not on the customer's phone)
- Review and approve content changes

## 8. Future expansion opportunities — retention is the business case

The bar owner's return on this app is repeat visits. Prioritize expansion by what brings a
member back in or tells the owner something actionable:

- Milestone badges at 25/50/100/150 and a shareable "mug earned" moment
- Seasonal mini-challenges (short repeatable clubs for finishers and slow movers)
- **Push notifications** (upgraded from in-app-only, July 2026): the frontend ships as an
  installable PWA with real web push — automated sends (new beers batched, "N to go"
  nudges, win-back after inactivity) plus owner-composed announcements targeted by
  audience; frequency-capped so members don't turn push off
- **Social layer** (July 2026, opt-in with display name): activity feed generated from
  real progress events, one-tap cheers, opt-in leaderboard among regulars, communal goal
  widget ("the bar has drunk N club beers this year" — TV-friendly), wall of mugs for
  finishers; free-text posting deliberately excluded to keep moderation near zero
- Personal beer journal: tasting notes (ratings and favorites graduated to My Beers and
  the want list, July 2026)
- Want-list on-tap push (added July 2026): a wanted beer flips to on-tap → automated
  targeted push to the members who want it, through the standard pipeline and caps
- Owner analytics: most/least confirmed beers (purchasing signal), member activity,
  lapsed-member list (promotion signal), want-list demand counts and anonymized average
  ratings per beer (added July 2026)
- Public ratings and reviews
- Advanced search and recommendations
- Native mobile app later if demand increases (the PWA covers install-to-home-screen and
  push in the meantime)
