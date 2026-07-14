# MVP Screen Plan

## 1. Mobile screens (the customer's phone is the primary device)

### Home / Dashboard (signed-in customer)
- My progress front and center: X of 200 with a progress ring/bar
- Search bar directly below — the fastest path from "I just ordered" to the beer's page
- What's new: recently added beers
- Next milestone (e.g. "7 beers to your 100 badge")

### Beer List / Search
- Search bar with autocomplete (name, brewery, style) — minimal typing, results as you type
- Filter chips: style, brewery, and **had / not had yet**
- Scrollable list of beers; each item shows name, brewery, style, and a checkmark if
  already confirmed for this customer

### Beer Detail
- Beer name, style, description from the tavern's list
- Brewery card enriched from Open Brewery DB: brewery type, city/state, website link
- Confirmed state (date + which visit) if the customer has had it
- **"I'm drinking this"** action — queues a confirmation request for the bartender
- No edit action here — catalog management is admin-only and lives on the laptop screens

### Login / Register
- Email and password form
- External login options if desired
- Clear error states

### My Progress (customer)
- Progress toward 200 (count and percent)
- Milestone badges earned (25/50/100/150) and next milestone
- List of confirmed beers with confirmation date
- List of remaining beers (links back into search/filter)
- "Mug earned" state once 200 is reached
- QR membership code for instant bartender lookup

### Confirm Beer (bartender, phone/tablet at the bar)
- Pending "I'm drinking this" requests as a one-tap approval queue (primary path)
- Manual path: search or select customer (or scan their QR code), then search or select beer
- Confirm action (records bartender identity + timestamp)
- Success state showing customer's updated count
- Designed to be fast — this replaces initialing a paper sheet, not a full admin form

## 2. Laptop admin screens

### Admin Dashboard
- Summary cards for key activity
- Quick links to manage beers and users
- Recent updates

### Beer Management Table
- List view of all beers
- Add, edit, delete actions (moved off the customer's phone — this is the only place
  catalog CRUD appears)
- Brewery autocomplete against Open Brewery DB when adding/editing a beer
- Search and filtering

### User Management
- List of users
- Role assignment (admin, bartender, customer)
- Account status

### Confirmation Audit (admin)
- List of bartender-confirmed beers per customer
- Ability to correct or remove a mistaken confirmation

## 3. Shared screens

- Error state
- Loading state
- Empty state
- Not found state

## 4. MVP priority order

1. Bartender: confirm beer for customer (the paper sheet replacement — nothing else matters
   until this works)
2. Customer: my progress (home screen)
3. Beer list with search and had/not-had filter
4. Beer detail screen with Open Brewery DB brewery info
5. Login / register (exists; needs logout + auth-aware navigation)
6. "I'm drinking this" request queue
7. Admin dashboard + beer management (CRUD relocates here from the customer surface)
8. User management
