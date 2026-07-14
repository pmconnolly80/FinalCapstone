# Feature Map

## 1. Core product features

### Mug club progress (primary driver)
- Customer sees which of the ~200 listed beers they've had and how many remain
- Bartender looks up a customer and confirms a specific beer for them (replaces the paper sheet initials — this is the step that actually marks a beer complete)
- **Bartender PIN confirmation on the customer's phone (decided July 2026)**: everything
  happens on the customer's phone — no bar tablet, no bartender screen. The customer finds
  the beer, taps "Confirm with bartender," and hands their phone over; the bartender types
  their personal 6-digit PIN, which both authorizes the confirmation and attributes it
  (`confirmed by Marco at 9:42pm` — the digital initials). PINs are hashed, validated
  server-side, rate-limited on two axes (per-PIN and per-customer-account), and managed by
  the owner/admin. See `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1 and `PERSONAS_AND_USAGE.md` §2
- Milestone/reward flag when a customer completes all 200 beers (mug earned)
- Admin can view/correct confirmation history (bartender error correction)
- Data model anticipates multiple taverns/locations, even though v1 targets one

### Catalog experience (search-first, on the customer's phone)
The app lives on the customer's phone, and search is the front door — the customer's core
loop is: search for the beer they're drinking → view its details → get it confirmed by the
bartender. Browsing supports discovery; search supports the moment of use at the bar.
- Search by name, brewery, or style — prominent, fast, minimal typing (autocomplete)
- Filter by style/brewery and by progress status (had / not had yet)
- View beer details built for beer nerds (expanded July 2026): style + style family and
  ale/lager class, ABV, IBU, description, and real brewery info (type, location, website)
  — auto-sourced from open projects (Open Brewery DB for the brewery, Catalog.beer as the
  beer-level candidate) so bartenders and the owner never have to type beer data; manual
  admin entry always remains available as fallback and override
- Browse beers, sort results
- **Open Brewery DB scope (verified July 2026):** the API provides *brewery* data only
  (name, type, address, geo, phone, website) — there is no beer-level endpoint. The tavern's
  own list remains the source of truth for the ~200 beers; Open Brewery DB enriches each
  beer's brewery details and powers brewery autocomplete when admins add beers — the point
  is that the admin doesn't hand-type brewery data and the customer sees real provenance info.
- **Catalog.beer (researched July 2026 — candidate):** free CC BY 4.0 API with actual
  *beer-level* data (style, ABV, IBU, description; ~60k beers) — the piece OBDB can't
  supply. Candidate for pre-filling beer fields when the admin adds a beer, pending a
  hit-rate spike against the tavern's real list. beer.db/openbeer.github.io was also
  evaluated and rejected (dormant since ~2015–2018). See `TECHNICAL_ARCHITECTURE_PLAN.md` §6.
- **Rotating inventory (added July 2026):** the bar's stock changes constantly, so every
  beer carries an availability state (on tap / available / out of stock / retired). Search
  and browse default to what's in stock now; retired beers stay in the catalog and in member
  histories — confirmations are permanent and progress never goes backwards when the list
  changes.

### Content management
- Create a beer record
- Edit a beer record
- Delete a beer record
- Manage metadata such as brewery and style

### User experience
- Register account
- Log in / log out
- **Social sign-in (added July 2026)**: sign in with Google, Facebook, or Apple as well as
  email/password — one tap on a barstool instead of inventing a password. Multiple
  providers can link to one account so progress never forks. Gives the bar a verified
  email and name for targeted marketing (with an explicit marketing-consent checkbox at
  signup). See `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6 for the researched options and
  recommendation
- Edit profile
- **My Beers (added July 2026)** — the completed list: every confirmed beer with its
  confirmation date, searchable and sortable (by date, name, style, my rating)
- **Ratings (added July 2026)** — rank your beers: a personal 1–5 star rating on any beer
  you've had confirmed (rating requires a confirmation — rankings stay tied to the club's
  integrity). Prompted right on the confirmation success screen ("How was it?"), editable
  later from beer detail or My Beers. Private by default; anonymized aggregates feed owner
  analytics
- **Want list (added July 2026)** — the "not sure what to order" answer (supersedes the
  earlier favorites/watchlists idea): add beers from search or detail, open it at the bar
  filtered to what's in stock tonight, and confirming a beer auto-checks it off
- **My Stats (added July 2026)** — beer-nerd visualizations of completions and ratings:
  progress over time, style-family breakdown, ABV distribution, rating distribution and
  average rating by style ("you rate saisons highest"), explored-vs-remaining by style
- View personal mug club progress
- "Confirm with bartender" — from a beer's detail page, a customer opens a full-screen
  PIN pad (beer and customer name shown large) and hands the phone across the bar; the
  bartender keys their 6-digit PIN and hands it back showing the updated count. Customer
  does all the finding; the bartender's entire interaction is six digits — the same
  gesture as initialing the old paper sheet. (Supersedes the earlier "I'm drinking this"
  request-queue design — with confirmation living on the customer's phone, there is no
  queue and no bartender device.)

### Engagement & retention (the business-owner case)
These are what make the app worth running for the bar, not just a digital scoresheet —
each one either brings a member back in or tells the owner something actionable:
- **Milestone badges** at 25/50/100/150 beers (not just the mug at 200) — smaller wins keep
  the long climb rewarding, with a shareable "mug earned" moment at the end
- **Seasonal mini-challenges** — short repeatable clubs (e.g. "12 IPAs of summer",
  Oktoberfest flight) that give finished/slow members a fresh reason to come in
- **Opt-in leaderboard** — friendly competition among regulars
- **Push notifications (expanded July 2026)** — real web push to the member's phone (the
  frontend becomes an installable PWA), with two senders: *owner-composed* messages from the
  owner dashboard (events, "new on tap Friday", win-backs) with audience targeting
  (all / active / lapsed / hasn't-had-beer-X), and *automated* sends (new beers batched,
  "only N to go" nudges, inactivity win-back). Frequency caps so members don't disable push.
  See `TECHNICAL_ARCHITECTURE_PLAN.md` §4.2
- **What's new / on tap feed** — the list changes; members should hear about it from the app
- **Personal beer journal** — tasting notes per beer, so the app is useful between visits
  (ratings and favorites graduated to first-class features July 2026: see My Beers and the
  want list under User experience)
- **Want-list on-tap push (added July 2026)** — when a beer on a member's want list flips
  to on-tap, they get an automated push: "*Beer X* you wanted is on tap tonight." Demand
  meets availability, automatically
- ~~Digital membership card (QR)~~ — superseded July 2026: with confirmation on the
  customer's own phone there is no bartender device to scan a code, and no lookup step at
  all (the customer's session already identifies them)
- **Owner analytics** — most/least confirmed beers (informs purchasing), member activity and
  progress distribution, lapsed-member list (informs promotions), plus marketing segments
  built from verified sign-in data + in-app behavior (favorite styles, visit cadence) for
  targeted push/email campaigns — consent-gated. Added July 2026: **want-list demand counts**
  ("31 members want beer X" — tap it and push the members who want it) and **anonymized
  average ratings per beer** as purchasing signals
- **Rewards beyond the mug** — birthday perk, halfway-club recognition, "second lap"
  double-mug club for finishers

### Social layer (added July 2026) — the club as a community, not 200 solo climbs
The members are all regulars at the same bar working the same list; the app should make
that visible. Everything here is **opt-in with a display name; default private**:
- **Activity feed** — system-generated posts from real progress events ("Dana hit 100",
  "Chris earned the mug 🏆", "6 new beers on the list"); no free-text posting, so
  moderation stays near zero
- **Cheers** — one-tap encouragement on a feed item, the app's only social verb at first
- **Opt-in leaderboard** — rank among regulars by confirmed count (also listed above under
  retention; it's the bridge feature between the two)
- **Communal goal widget** — "the bar has drunk 4,812 club beers this year"; owner can put
  it on the TV over the taps
- **Wall of mugs** — permanent roll of members who finished the 200
- Later: communal/team challenges, milestone sharing outside the app, referral incentives
- See `TECHNICAL_ARCHITECTURE_PLAN.md` §4.3 for the data model and
  `PERSONAS_AND_USAGE.md` for how it plays at the bar

### Administration
- Manage users
- Manage roles and permissions (admin, bartender, customer)
- Issue, reset, and deactivate bartender PINs (part of staff on/offboarding)
- Manage beer availability as inventory rotates (on tap / out of stock / retired)
- Moderate the social layer (display names, hide a feed item)
- Review content submissions
- Moderate records
- **Full data correction (added July 2026)**: the admin can edit *all* data — beer
  records, confirmations, user accounts, social content — to fix inaccuracies or
  questionable submissions. Every correction is audited (who changed what, when, with a
  required reason note); nothing is silently deleted. This is the app's ultimate
  backstop: whatever goes wrong at the bar, an admin can put the data right
- **Anomaly alerts (added July 2026)**: unusual activity notifies the owner and admin
  automatically — an abnormal burst of beers added to the catalog at once, confirmation
  velocity spikes, or off-hours activity. Informational, not auto-blocking; pairs with
  the audit trail so questionable data can be reviewed and corrected deliberately

## 2. Phone-first user stories

The app's home is the customer's phone at the bar. The defining loop:

- As a customer sitting at the bar, I can search for the beer I'm drinking in a few
  keystrokes, open it, and see its details (style, description, brewery info from Open
  Brewery DB) before or while I drink it.
- As a customer, I can tap "Confirm with bartender" on the beer I'm drinking, hand my
  phone across the bar, and get it back with my count ticked up — I do all the
  search-and-select work; the bar staff never needs a device of their own.
- As a bartender, my entire interaction is typing my personal 6-digit PIN on the
  customer's phone — under five seconds, wet hands and all, and the confirmation is
  recorded under my name like initials on the paper sheet.
- As a customer, I can view my mug club progress (X of 200) on my phone, with the beers
  I still need clearly filterable in the list.
- As a customer, I see what's actually in stock tonight by default when I search, so a big
  rotating catalog never buries the beers I can really order.
- As a customer who isn't sure what to order, I open my want list filtered to what's in
  stock tonight and pick from beers I already decided I want.
- As a customer, right after a beer is confirmed I'm asked "How was it?" and can tap a
  1–5 star rating — and change it later from My Beers or the beer's page.
- As a beer nerd, I can open My Stats and see visualizations of everything I've completed
  and how I rated it — styles explored, ABV spread, what I rate highest.
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
- Bartender PIN entry on the customer's phone (completes the confirmation flow's
  real-world usability — the confirm endpoint should carry the `pin` field from Sprint 1)
- Beer availability states + in-stock-by-default search (the rotating inventory reality)
- Milestone badges (25/50/100/150) and "mug earned" state
- Better mobile form experience
- Admin full-data correction tools (confirmations first, then all records) with audit trail
- Admin catalog management (create/edit/delete moves here, out of the customer surface)
  with PIN management, availability management, and the bulk-add anomaly alert

### Priority 3 — retention, social, and owner value
- Push notifications (PWA install, subscriptions, automated sends, owner composer with
  audience targeting)
- My Beers: completed list with 1–5 ratings (rate-after-confirm prompt), want list with
  in-stock filter and on-tap push trigger, My Stats visualizations
- Social v1: opt-in profile/display name, milestone activity feed, cheers, leaderboard,
  communal goal widget, wall of mugs
- Seasonal mini-challenges
- Personal beer journal (tasting notes — ratings/favorites now live in My Beers/want list)
- Owner analytics (beer popularity, member activity, lapsed members, want-list demand,
  anonymized average ratings)
- Improved admin dashboard
