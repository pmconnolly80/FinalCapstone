# Feature Map

## 1. Core product features

### Mug club progress (primary driver)
- Customer sees which of the ~200 listed beers they've had and how many remain
- Bartender looks up a customer and confirms a specific beer for them (replaces the paper sheet initials — this is the step that actually marks a beer complete)
- Milestone/reward flag when a customer completes all 200 beers (mug earned)
- Admin can view/correct confirmation history (bartender error correction)
- Data model anticipates multiple taverns/locations, even though v1 targets one

### Catalog experience (search-first, on the customer's phone)
The app lives on the customer's phone, and search is the front door — the customer's core
loop is: search for the beer they're drinking → view its details → get it confirmed by the
bartender. Browsing supports discovery; search supports the moment of use at the bar.
- Search by name, brewery, or style — prominent, fast, minimal typing (autocomplete)
- Filter by style/brewery and by progress status (had / not had yet)
- View beer details: style, description, and real brewery info (location, website) enriched
  from Open Brewery DB
- Browse beers, sort results
- **Open Brewery DB scope (verified July 2026):** the API provides *brewery* data only
  (name, type, address, geo, phone, website) — there is no beer-level endpoint. The tavern's
  own list remains the source of truth for the ~200 beers; Open Brewery DB enriches each
  beer's brewery details and powers brewery autocomplete when admins add beers.

### Content management
- Create a beer record
- Edit a beer record
- Delete a beer record
- Manage metadata such as brewery and style

### User experience
- Register account
- Log in / log out
- Edit profile
- View personal saved items or favorites
- View personal mug club progress
- "I'm drinking this" — from a beer's detail page, a customer flags the beer they're
  currently drinking, which queues a confirmation request the bartender approves with one
  tap (customer searches → bartender confirms; keeps the bartender-gated rule while cutting
  the bartender's data entry to a single approval)

### Engagement & retention (the business-owner case)
These are what make the app worth running for the bar, not just a digital scoresheet —
each one either brings a member back in or tells the owner something actionable:
- **Milestone badges** at 25/50/100/150 beers (not just the mug at 200) — smaller wins keep
  the long climb rewarding, with a shareable "mug earned" moment at the end
- **Seasonal mini-challenges** — short repeatable clubs (e.g. "12 IPAs of summer",
  Oktoberfest flight) that give finished/slow members a fresh reason to come in
- **Opt-in leaderboard** — friendly competition among regulars
- **Notifications** — new beers added to the list, "only N beers to go" nudges, win-back
  messages after inactivity, event announcements
- **What's new / on tap feed** — the list changes; members should hear about it from the app
- **Personal beer journal** — favorites, private ratings, and tasting notes per beer, so the
  app is useful between visits
- **Digital membership card (QR)** — customer shows a code, bartender's lookup is instant
- **Owner analytics** — most/least confirmed beers (informs purchasing), member activity and
  progress distribution, lapsed-member list (informs promotions)
- **Rewards beyond the mug** — birthday perk, halfway-club recognition, "second lap"
  double-mug club for finishers

### Administration
- Manage users
- Manage roles and permissions (admin, bartender, customer)
- Review content submissions
- Moderate records
- Correct or audit bartender beer confirmations

## 2. Phone-first user stories

The app's home is the customer's phone at the bar. The defining loop:

- As a customer sitting at the bar, I can search for the beer I'm drinking in a few
  keystrokes, open it, and see its details (style, description, brewery info from Open
  Brewery DB) before or while I drink it.
- As a customer, I can tap "I'm drinking this" so the bartender only has to approve it —
  I do the search-and-select work on my own phone, not the bartender on theirs.
- As a customer, I can view my mug club progress (X of 200) on my phone, with the beers
  I still need clearly filterable in the list.
- As a customer, I can show a QR membership code so the bartender finds my account instantly.
- As a bartender, I can quickly look up a customer and confirm a beer for them from a phone
  or tablet at the bar — this needs to be fast enough to use mid-shift, not a
  laptop-admin-style workflow. Pending "I'm drinking this" requests appear as a one-tap
  approval queue.
- As a user, I can browse the beer catalog quickly from my phone.

Note create/edit/delete of beers is deliberately *not* a phone-first story anymore — catalog
management is an admin/laptop concern and should stop occupying prime real estate in the
customer-facing navigation.

## 3. Laptop admin stories

- As an admin, I can see a list of beers and manage them efficiently.
- As an admin, I can review content changes in a structured dashboard.
- As an admin, I can manage users and permissions from a desktop interface.

## 4. Later-phase features

- Ratings and reviews (public — private tasting notes land earlier, see Engagement & retention)
- Recommendations
- Image uploads
- Full analytics dashboards
- Social sharing of milestones, referral incentives

## 5. Suggested MVP feature priority

### Priority 1 — the core loop on a phone
- Authentication (customer, bartender, admin roles)
- Bartender beer-confirmation flow
- Customer mug club progress view
- Search the tavern's list (name/brewery/style, with had / not-had filter) — this is the
  customer's entry point at the bar, not a nice-to-have
- Beer details enriched with Open Brewery DB brewery info
- Beer list / browse

### Priority 2 — friction removal and stickiness
- "I'm drinking this" confirmation-request queue
- QR membership code for customer lookup
- Milestone badges (25/50/100/150) and "mug earned" state
- Better mobile form experience
- Bartender-confirmation error correction/audit tools
- Admin catalog management (create/edit/delete moves here, out of the customer surface)

### Priority 3 — retention and owner value
- Notifications (new beers, nudges, win-back)
- Seasonal mini-challenges, opt-in leaderboard
- Personal beer journal (favorites, notes, private ratings)
- Owner analytics (beer popularity, member activity, lapsed members)
- Improved admin dashboard
