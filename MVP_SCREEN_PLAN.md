# MVP Screen Plan

## 1. Mobile screens (the customer's phone is the primary device)

### Home / Dashboard (signed-in customer)
- My progress front and center: X of 200 with a progress ring/bar
- Search bar directly below — the fastest path from "I just ordered" to the beer's page
- What's new: recently added beers
- Next milestone (e.g. "7 beers to your 100 badge")

### Beer List / Search
- Search bar with autocomplete (name, brewery, style) — minimal typing, results as you type
- **Defaults to what's in stock now** — the inventory rotates and the catalog outgrows 200
  rows, so availability (on tap / out of stock / retired) is a first-class filter
- Filter chips: style, brewery, availability, and **had / not had yet**
- Scrollable list of beers; each item shows name, brewery, style, availability badge, and
  a checkmark if already confirmed for this customer

### Beer Detail
- Beer name, style, description from the tavern's list, availability state
- Beer-nerd stats block: ABV, IBU, style family + ale/lager class (auto-sourced where
  possible — Catalog.beer candidate — admin-editable always)
- Brewery card enriched from Open Brewery DB: brewery type, city/state, website link
- Later: one-paragraph style primer ("what is a saison?") from open style-guideline data
- Confirmed state (date + which visit) if the customer has had it, with my 1–5 star
  rating shown and editable
- **"Add to want list"** action (hidden once confirmed)
- **"Confirm with bartender"** action — opens the Confirmation PIN Pad (below)
- No edit action here — catalog management is admin-only and lives on the laptop screens

### Login / Register
- Social sign-in buttons first — Google, Facebook, Apple (one tap on a barstool beats
  inventing a password); email/password form as the fallback
- Marketing-consent checkbox at registration (stored; feeds the owner's targeted
  marketing, see `TECHNICAL_ARCHITECTURE_PLAN.md` §4.6)
- Account linking so a member can attach several providers to one progress record
- Clear error states

### My Progress (customer)
- Progress toward 200 (count and percent)
- Milestone badges earned (25/50/100/150) and next milestone
- Links into My Beers (the full completed list) and the remaining beers (back into
  search/filter)
- "Mug earned" state once 200 is reached

### My Beers (customer)
- Every confirmed beer with its confirmation date and my 1–5 star rating
- Search within it; sort by date, name, style, or my rating ("my ranking" view)
- Tap through to beer detail to edit a rating or add tasting notes

### Want List (customer)
- The "not sure what to order" screen: beers I've saved, with an **in-stock tonight**
  filter on by default so it's an actionable menu at the bar
- Add from search results or beer detail; remove anytime
- Confirmed beers auto-check off (with a small "had it ✓" moment)
- Each item shows availability badge; wanted beers that come on tap trigger a push

### My Stats (customer — the beer-nerd payoff)
- Progress over time (cumulative confirmations)
- Style-family breakdown of completions, and explored-vs-remaining by style
  ("you've had 80% of the IPAs, 10% of the sours")
- ABV distribution of what I've drunk
- Rating distribution and average rating by style ("you rate saisons highest")
- All from one `GET /api/me/stats` call, rendered as lightweight charts

### Confirmation PIN Pad (on the customer's phone — the one-device rule)
- Full-screen takeover launched from "Confirm with bartender" on beer detail
- Beer name and customer name displayed large, so the bartender verifies at a glance
- 6-digit masked PIN pad for the bartender — the customer hands the phone across the bar,
  the bartender keys their personal PIN, done (this *is* the bartender's entire UI; there
  is no bar tablet, no bartender screen, no request queue)
- Server-side validation; lockout/cooldown messaging on repeated failures
- Success state hands the phone back showing the updated count (and milestone, if
  crossed), a **"How was it?" 1–5 star rating prompt** (skippable, editable later), and a
  want-list check-off note if the beer was on it
- Designed to be as fast as initialing the paper sheet: one tap + six digits

### Social (opt-in, later phase)
- Activity feed: system-generated member milestones ("Dana hit 100", "Chris earned the
  mug"), new-beer announcements; one-tap **cheers** on any item
- Leaderboard among opted-in members; communal goal widget; wall of mugs
- Profile: choose display name, opt in/out of the social surface

## 2. Laptop admin screens

### Admin Dashboard
- Summary cards for key activity
- Quick links to manage beers and users
- Recent updates
- **Anomaly panel**: bulk beer-add alerts, confirmation velocity spikes, off-hours
  activity — each item links to the relevant audit/correction screen
- **Purchasing signals** (owner): want-list demand counts per beer ("31 members want
  beer X") and anonymized average ratings — what to tap next, straight from member data

### Beer Management Table
- List view of all beers with **availability state** (on tap / available / out of stock /
  retired) editable inline — the rotating-inventory workflow lives here
- Add, edit, delete actions (moved off the customer's phone — this is the only place
  catalog CRUD appears)
- Brewery autocomplete against Open Brewery DB when adding/editing a beer (fills brewery
  name/type/location/website so the admin doesn't hand-type it)
- Search and filtering

### User Management
- List of users
- Role assignment (admin, bartender, customer)
- Bartender PIN management: issue, reset, deactivate (tied to on/offboarding)
- Account status

### Owner: Notification Composer
- Compose a push notification (title + message), pick an audience: all / active /
  lapsed / hasn't-had-beer-X
- Send now or schedule; preview before sending
- Past sends with delivery counts

### Data Correction & Audit (admin — edit everything, silently delete nothing)
- Admin can edit **any** record — beers, confirmations, user accounts, social content —
  to correct inaccuracies or questionable submissions
- Confirmation history per customer: beer, timestamp, confirming bartender; correct or
  remove a mistaken confirmation
- Every correction requires a reason note and lands in an audit log (who, what, when, why)

## 3. Shared screens

- Error state
- Loading state
- Empty state
- Not found state

## 4. MVP priority order

1. Confirmation on the customer's phone (the paper sheet replacement — nothing else
   matters until this works), with the bartender PIN pad completing it
2. Customer: my progress (home screen)
3. Beer list with search, availability default, and had/not-had filter
4. Beer detail screen with Open Brewery DB brewery info
5. Login / register (exists; needs logout, auth-aware navigation, then social sign-in)
6. Admin dashboard + beer management (CRUD relocates here from the customer surface,
   availability states, bulk-add anomaly alert)
7. User management + bartender PIN management
8. Data correction & audit (admin edit-everything backstop)
9. Owner notification composer (with push infrastructure)
10. My Beers + ratings (rate-after-confirm), Want List (in-stock filter + on-tap push),
    My Stats visualizations
11. Social layer (feed, cheers, leaderboard)
