# Mobile-First Product Outline

## 1. Product goal

Build a mobile-first beer application that is easy to use on a phone, while also supporting management workflows on a laptop.

Concretely: digitize a tavern's paper mug-club sheet, where a bartender initials next to a beer once a customer has had it. The app needs a fast, at-the-bar bartender flow (phone/tablet, not laptop) to confirm a beer for a customer, plus a customer-facing progress view toward the 200-beer goal.

## 2. Core usage pattern

The moment that defines the product happens at the bar: a customer orders a beer, pulls out
their phone, **searches** for that beer on the tavern's list, reads about it, and gets it
counted toward their 200. Everything else supports that moment.

- Primary customer interaction happens on a phone (search, beer info, viewing progress)
- Search is the customer's front door — finding the beer they're drinking in a few
  keystrokes, not scrolling a 200-item list
- Beer details are worth reading: style and description from the tavern's list plus real
  brewery info (location, website) pulled from Open Brewery DB
- Confirmation stays bartender-gated, but the customer does the finding: "I'm drinking
  this" on the customer's phone queues a request the bartender approves with one tap
- Bartender confirmation happens on a phone or tablet at the bar — it needs to be quick enough to use mid-shift, closer to a point-of-sale interaction than a content-management one
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
  is real and consistent instead of free-typed.

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
- Beer detail page — style, description, brewery info from Open Brewery DB, and an
  "I'm drinking this" action that queues a bartender confirmation request
- Browse beer catalog
- My mug club progress (X of 200, remaining beers, milestone badges)
- Account / profile with QR membership code for instant bartender lookup
- Bartender: customer lookup + confirm beer, including a pending-requests approval queue
  (separate fast-path flow, not part of admin)

Deliberately removed from the customer's mobile surface: beer create/edit/delete. Catalog
management is an admin task and shouldn't sit in the customer's navigation.

## 4. Admin experience on laptop

### Goals
- Efficient management of catalog data
- Simple moderation and oversight tools
- Clear views for users, roles, and content

### Laptop-first screens
- Admin dashboard
- Beer management table
- Create / edit / delete workflows
- User management
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
- Log in / sign up
- Customer: view mug club progress toward 200
- Bartender: look up a customer and confirm a beer for them

### Must-have laptop flows
- Admin dashboard
- Manage beer records (create/edit/delete lives here, not on the customer's phone)
- Review and approve content changes

## 8. Future expansion opportunities — retention is the business case

The bar owner's return on this app is repeat visits. Prioritize expansion by what brings a
member back in or tells the owner something actionable:

- "I'm drinking this" confirmation-request queue (customer finds, bartender one-tap approves)
- Milestone badges at 25/50/100/150 and a shareable "mug earned" moment
- Seasonal mini-challenges (short repeatable clubs for finishers and slow movers)
- Notifications: new beers on the list, "N to go" nudges, win-back after inactivity
- Opt-in leaderboard among regulars
- Personal beer journal: favorites, tasting notes, private ratings
- Owner analytics: most/least confirmed beers (purchasing signal), member activity,
  lapsed-member list (promotion signal)
- Public ratings and reviews
- Advanced search and recommendations
- Native mobile app later if demand increases
