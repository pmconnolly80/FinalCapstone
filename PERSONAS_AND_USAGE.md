# Personas & Usage Deep Dive

How the app is actually used, persona by persona. This doc expands the July 2026 feature
ideas — bartender PIN confirmation on the customer's phone, owner push notifications, a
social layer among members, and a large rotating inventory — into concrete day-in-the-life
usage, so the screens, data model, and backlog stay grounded in real behavior at the bar.
Companion docs: `FEATURE_MAP.md` (what), `TECHNICAL_ARCHITECTURE_PLAN.md` (how),
`MVP_SCREEN_PLAN.md` (where on screen).

**The one-device rule (decided July 2026): everything happens on the customer's phone.**
There is no bar tablet, no bartender confirmation device, no queue screen. The bartender's
entire interaction with the app is typing their personal 6-digit PIN into the customer's
phone — the digital equivalent of initialing the customer's paper sheet, which was also
the customer's artifact, handed across the bar.

Four personas, four very different relationships with the same data:

| Persona | Device | Frequency | What the app is to them |
|---|---|---|---|
| Customer (member) | Own phone | Every visit + between visits | Their scoresheet, beer guide, and clubhouse |
| Bartender | The customer's phone (PIN pad) | Constantly mid-shift | A six-digit signature — never a device or screen of their own |
| Owner | Laptop (occasionally phone) | Weekly | A megaphone (push) and a dashboard (analytics) |
| Admin | Laptop | As needed | Catalog upkeep, accounts/PINs, and the fix-it desk |

Owner and Admin may be the same human at a small tavern, but they are different *hats*:
the owner hat makes business decisions and talks to members; the admin hat maintains data.

**Revised 2026-07-23** (a 2026-07-23 review found the code had already quietly merged
these into one `Admin` role, contrary to this doc's original "keep them separable"
framing — see `USABILITY_TESTING.md`): rather than a strict Owner/Admin permission
split, the decided direction is **multiple Admin accounts, individually attributed**
(every audited action already records its actor via `AdminAudit` — this is mostly
already true), plus **one top-level account that can provision the others** — e.g. the
tavern owner creates Admin accounts for staff who do data upkeep, without granting
them the ability to create further Admin accounts themselves. Not yet built; see
`TECHNICAL_ARCHITECTURE_PLAN.md` and `IMPLEMENTATION_BACKLOG.md` for backlog placement.

---

## 1. Customer — "Dana", a regular chasing the mug

### First visit with the app
Dana signs up at the bar — one tap on **"Continue with Google"** (email/password exists,
but nobody wants to invent a password on a barstool; the bar gets a verified email, and
Dana ticks — or doesn't — the marketing-consent box). She lands on a home screen that says
**0 of 200**
with a search bar under it. The bartender explains the club works like the old paper sheet:
find your beer, hand me your phone, I'll sign it. Dana orders a saison, types "sais…" into
search, opens the beer, and reads the description — plus a brewery card (city, type,
website) pulled from Open Brewery DB, so the beer's origin story is real data, not whatever
an admin had time to type. The detail page is built for beer nerds: ABV, IBU, style
family, the works — auto-sourced so it's actually filled in, not blank fields nobody had
time to type. Dana taps **"Confirm with bartender"**, a full-screen PIN pad appears, and
Dana slides the phone across the bar. Marco keys in his 6-digit PIN — two seconds — and
hands it back showing **1 of 200**. That tick is the hook.

### A normal bar night
- Orders → searches the tavern's list → the **available-now filter is on by default**, so a
  400-beer historical catalog doesn't bury the 180 beers actually in stock tonight.
- Filters "not had yet" to pick the next beer — the app turns "what should I order?" into
  a progress move. Genuinely stuck? She opens her **Want List** — beers she saved on
  earlier nights and couch-browsing sessions — filtered to what's in stock tonight. A
  personal menu she already agreed with.
- Taps "Confirm with bartender" and passes the phone when the beer lands. The whole
  exchange rides the same gesture as handing over the old paper sheet. When the phone
  comes back, the success screen asks **"How was it?"** — one tap, four stars, done (and
  if the beer was on her want list, it checks itself off with a little "had it ✓").
- Hits 50: a badge animates, and — because Dana opted into the social feed — the feed posts
  "Dana hit 50 🍺". Two other regulars at the bar tap **cheers** on it. One of them is three
  beers ahead; Dana checks the leaderboard and orders one more.

### Between visits (this is where retention lives)
- A push notification lands Thursday: *"6 new beers on tap this weekend — 4 you haven't
  had."* Written by the owner, targeted by the system (it knows Dana's remaining list).
- Dana opens the app on the couch: rates the two beers from last night she skipped rating,
  browses the journal (tasting notes on that saison), adds three intriguing beers to her
  **want list** for next time, and checks the feed (a regular earned their mug last night
  — the app posted the shout-out). "Next milestone: 3 beers to 100."
- She nerds out on **My Stats**: she's had 80% of the IPAs but only 10% of the sours (a
  plan forms), her ABV histogram skews stronger than she'd admit, and apparently she
  rates saisons highest. Her completed list, her rankings, her data — this is the part of
  the app that's *hers*.
- Tuesday, a push: *"Fantôme Saison — a beer on your want list — is on tap tonight."*
  That one notification is the whole retention thesis in one sentence.
- After three quiet weeks, a win-back push: *"Your mug misses you — still 27 to go."*

### Edge cases that must not break Dana's trust
- A beer she drank rotates **out of stock** or is **retired** from the list: her
  confirmation is permanent. Progress never goes backwards because inventory changed.
- The wrong beer gets confirmed in the Friday rush: Dana sees it on her confirmed list,
  mentions it, and an admin corrects it from the audit screen — visible in her history as
  corrected, never silently deleted.
- Social is **opt-in**: until Dana chooses a display name and opts in, nothing about her
  appears in any feed or leaderboard.
- Dead phone night: the club is phone-based by design; the tavern decides whether "tell
  the bartender, admin back-fills it tomorrow" is house policy (see open questions).

---

## 2. Bartender — "Marco", mid-Friday-rush

### The reality the design must respect
Marco has wet hands, three orders queued, and no patience for screens of his own. He will
not carry a device, watch a queue, or log into anything mid-shift. The design gives him
nothing to operate: the customer does all the finding on their own phone, and Marco's whole
job is a six-digit signature.

### The PIN moment (the core mechanic)
- Dana's phone arrives across the bar already showing the PIN pad, beer name and customer
  name large at the top: *Saison Dupont — Dana*.
- Marco glances (right beer? this customer's account?), keys his **personal 6-digit PIN**,
  and slides the phone back. The record stores *confirmed by Marco, 9:42pm* — exactly his
  initials on the paper sheet, but attributable and timestamped.
- Total interaction: read, six digits, done. Under five seconds, no device of his own.

### What keeps Marco's PIN safe on someone else's phone
He's typing his credential into a customer's device, so the system protects him:
- The pad shows dots, never digits; the PIN is verified server-side and never stored on or
  returned to the phone.
- Repeated wrong PINs lock the customer's confirmation flow (cooldown), so nobody can sit
  and guess.
- Velocity caps flag the impossible — a customer account "drinking" ten club beers in an
  hour, or confirmations while the bar is closed — on the owner's dashboard, attributed
  per-bartender. If Dana ever shoulder-surfed and reused his PIN, the pattern surfaces.
- Marco can change his PIN anytime by logging into his own account on his own phone, and
  the owner rotates PINs periodically as policy.

### What Marco does *not* do
- No catalog editing, no lookup screens, no analytics, no queue. If a beer isn't on the
  list, that's an admin's job tomorrow — Marco says "it'll count once it's added" (admin
  can back-date a correction if the tavern wants that policy).
- Wrong confirmation? Corrections go through the admin audit trail — no silent deletes,
  which protects Marco as much as the customer.

### PIN lifecycle
Owner/admin issues Marco a PIN when he's hired (he changes it on first use), resets it if
forgotten, deactivates it the day he leaves. A departed bartender's PIN stops working
everywhere instantly — something a laminated paper sheet never offered.

**Under review 2026-07-23:** onboarding today actually requires Marco to self-register
like a customer *before* an admin can find and promote him — not the "one screen" this
implies. A lighter model has been floated where Marco never has an account or logs in
at all: the admin creates his staff record and PIN directly, using his birthday
(`MMDDYYYY`) as an easy-to-remember 8-digit PIN instead of a random 6-digit one. Not
decided — needs a real design pass (PIN length is hardcoded to 6 digits in several
places, and this would decouple `StaffPin` from a full Identity account). See
`TECHNICAL_ARCHITECTURE_PLAN.md` §4.1's "Open architecture questions" and
`USABILITY_TESTING.md`.

---

## 3. Owner — "Terri", who pays for all this

Terri's question is always the same: *is the club putting people on stools?*

### Weekly ritual (laptop, Monday morning)
- **Dashboard** (revised 2026-07-23 — see `USABILITY_TESTING.md`): the shipped Admin
  Dashboard (#59) covers **operational health only** — total beers, confirmations
  today, active members, mugs awarded, plus the anomaly panel. It deliberately does
  *not* yet answer Terri's real "what should I order more of / who's about to lapse"
  question below — that's explicitly deferred to a separate, later **Owner Analytics**
  screen once the Engagement/Retention epic is groomed, rather than implied to already
  be part of today's dashboard.
- **Beer intelligence** (Owner Analytics — not yet built): most- and least-confirmed
  beers. The stout nobody's ordered in two months informs the next distributor order;
  the hazy IPA that's carrying the month suggests going deeper on that style. The
  rotating inventory finally produces data instead of gut feel. Added July 2026:
  **want-list demand counts** ("31 members want beer X" — put it on tap, and the app
  pushes exactly those 31 members) and **anonymized average ratings** per beer —
  members tell her what to buy without being asked. Most/least-confirmed beers is a
  cheap first slice (a simple query over existing `BeerConfirmation` rows, no new
  schema) worth pulling forward ahead of the rest of this list.
- **Trust check**: the anomaly panel — off-hours confirmations, velocity spikes, one
  bartender's numbers looking unlike the others', and **unusual catalog activity** (a burst
  of beers added at once). Usually empty; priceless when it isn't. The catalog alert also
  arrives as a notification the moment it fires, not just on Monday.

### The megaphone: composing a push notification
From the owner dashboard, Terri writes a push notification the way she'd write a chalkboard:
- Picks an audience: **everyone**, **active members**, **lapsed members**, or **members who
  haven't had beer X** (the system joins her message against progress data). Audiences are
  built from what the app verifiably knows — sign-in identity (social sign-in gives a real
  name and deliverable email) plus in-app behavior (favorite styles, visit cadence) — and
  only include members who ticked the marketing-consent box.
- Types the message: *"Firkin Friday: cask bitter on at 5. It's on the list, people."*
- Previews and sends (or schedules for Thursday afternoon — when weekend plans get made).
- Sees basic results later: delivered count, and whether tapped-through members confirmed
  a beer that weekend.

Automated notifications (milestone nudges, new-beer announcements, win-backs) run without
her — she sets the tone once, the system handles the timing. Frequency caps protect her
members from notification fatigue; an over-pinged member disables push and she loses the
channel forever.

### Seasonal levers
Terri spins up a mini-challenge ("Oktoberfest: 6 German styles in October") to re-activate
finishers and slow movers, and watches the communal goal widget — *"The bar has collectively
drunk 4,812 beers this year"* — which she also puts on the TV over the taps.

---

## 4. Admin — "Sam", keeper of the list

### Rotating inventory is Sam's main job
The list is large and changes constantly — that's the tavern's identity. Sam's flow when
the Tuesday delivery lands:

1. **Add a beer** (laptop, beer management table): types the beer's name, then in the
   brewery field types "All…" — autocomplete against **Open Brewery DB** suggests
   *Allagash Brewing Company, Portland, ME*. One click fills brewery name, type, city,
   state, website — real data, zero typing, consistent spelling forever. (OBDB is a
   *brewery* directory only — beer-level fields come from the second lookup below.)
2. **Beer-level pre-fill** (Catalog.beer, researched July 2026, pending a hit-rate spike
   against the tavern's real list): the name search also queries Catalog.beer and
   pre-fills style, ABV, IBU, and description for Sam to verify — the beer-nerd data
   customers actually read, without staff typing it. The stated principle
   (`TECHNICAL_ARCHITECTURE_PLAN.md` §6): **auto-enrich first, manual entry as the
   fallback and override** — for the beers no source knows (the local nano-brewery's
   one-off cask), Sam types it by hand, and every auto-filled field stays editable
   because the tavern's list is the source of truth.
3. **Flip availability**: kicked kegs → *out of stock*; seasonal gone for good → *retired*.
   Retired beers stay in the catalog and in members' histories — they just leave the
   default search results.
4. New beers trigger the automated *"new on the list"* notification (batched, not one push
   per beer).

**Guardrail on catalog writes (added July 2026):** adding an unusually large number of
beers in a short window automatically notifies both the owner and admin — "this is not
normal behavior and might need a look." A Tuesday delivery batch of eight is normal; a
3am burst of sixty is a compromised account or a runaway import. The alert is informational
(nothing is blocked or rolled back automatically); the audit trail records who added what,
so an admin can review and clean up deliberately.

### Accounts, roles, PINs
- Promotes a new hire to Bartender role and issues their first 6-digit PIN; deactivates
  both when staff leave (one screen, because it will be forgotten otherwise).
- Resets forgotten PINs (Marco, every few months) and runs the periodic rotation the
  owner sets as policy.

### The fix-it desk: full data correction
- **Sam can edit any data in the system** — beer records, confirmations, user accounts,
  social content — to correct inaccuracies or questionable submissions (decided July
  2026). The admin is the app's backstop: whatever goes wrong at the bar, the data can be
  put right.
- Views any customer's confirmation history: beer, timestamp, confirming bartender.
- Corrects mistakes — wrong beer, wrong customer, duplicate — with a required reason note.
  Corrections are logged, never silent deletes: the audit trail is the digital version of
  "crossed out and re-initialed" on the paper sheet.
- Anomaly alerts (bulk beer-adds, velocity spikes) point Sam at what to review; the
  edit-everything capability is how the review turns into a fix.
- Moderates the social layer: display names and feed content are member-visible, so Sam can
  rename/mute an offensive display name and remove a feed item.

---

## 5. Cross-persona flows (how the features interlock)

### The confirmation moment (all four features touch it)
```
Dana searches beer on her phone (available-now filter, OBDB-enriched detail)
  → taps "Confirm with bartender" → PIN pad fills her screen (beer + name shown large)
  → hands phone to Marco → Marco keys his 6-digit PIN → phone back to Dana
  → BeerConfirmation recorded (customer from session, bartender resolved from PIN, timestamp)
  → want-list item auto-checks off ("had it ✓") → "How was it?" one-tap 1–5 rating
  → Dana's count ticks up → milestone? → badge + opt-in feed post + cheers
  → 200? → MUG EARNED → feed shout-out + owner dashboard flag (Terri hands over the mug)
```

### The want-list loop (demand meets availability)
```
Dana saves beers to her Want List (couch browsing, slow nights)
  → Terri's dashboard aggregates demand ("31 members want beer X") → informs the order
  → Sam flips beer X to on-tap → targeted push to exactly those members
  → Dana comes in, opens Want List (in-stock filter), orders → confirmation loop above
  → her rating lands in the anonymized averages Terri buys against next month
```

### The new-beer lifecycle
```
Delivery arrives → Sam adds beer (OBDB brewery autocomplete) + sets availability
  → batched "new on the list" push (owner-toned, system-sent)
  → Dana filters "new / not had" → orders → confirmation loop above
  → Terri's dashboard shows which new beers actually get confirmed
```

## 6. Confirmation alternatives considered (single-phone constraint held)

PIN-on-the-customer's-phone is the chosen v1. Options if it proves awkward in practice —
all still customer-phone-only, kept here so the exploration isn't lost:

1. **Rotating bartender code (TOTP-style)** — the bartender's own phone/watch shows a
   6-digit code that changes every 30s; they read it out or type it. Kills the
   shoulder-surf risk of a static PIN, but reintroduces a bartender device to glance at.
2. **Bartender approves from their own phone** — customer's tap queues a request; the
   bartender confirms on their device. Rejected as the primary flow (bartenders won't
   watch a queue mid-shift; violates the one-device rule) but architecturally cheap to
   add later as an opt-in for bartenders who prefer it.
3. **NFC staff tap** — bartender taps an NFC staff card to the customer's phone. Dead on
   arrival for v1: Web NFC doesn't work on iPhones in the browser.
4. **Trust-the-customer + spot audit** — no gate at all, anomaly detection after the fact.
   Rejected: bartender confirmation *is* the club's integrity story, same as the paper
   initials.

## 7. Open product questions (to confirm with the tavern, not to code around)

1. **Is the goal a fixed 200 or "the whole current list"?** Recommendation: fixed 200
   distinct confirmed beers, independent of list size — simple, matches the club's name,
   and immune to inventory churn.
2. **Do beers retired before a member started still count if re-confirmed from a bottle
   share?** Recommendation: only available beers can be newly confirmed; past confirmations
   are permanent.
3. **Leaderboard identity**: display names only, opt-in, default private. Confirm the owner
   is comfortable moderating.
4. **PIN policy**: 6 digits (decided); confirm lockout threshold, cooldown length, and
   rotation cadence with the owner.
5. **Dead-phone policy — decided 2026-07-23**: no backfill capability for v1. A
   customer with no signal or a dead phone simply can't get that beer confirmed that
   night; the app shows a clear in-app message ("no signal — ask the bartender to note
   it") rather than failing silently or looking broken. Admin backfill was considered
   and explicitly deferred, not built. See `USABILITY_TESTING.md`.
6. **Push tone and frequency caps**: how many owner-composed sends per week is too many?
7. **First-time acquisition — decided 2026-07-23**: a QR code (table-tent/coaster,
   physical/marketing, outside this codebase) pointing at an in-app
   `/auth?mode=register` entry point is the v1 answer. Native app-store presence
   (Google Play / Apple App Store) is a genuine future direction to explore — logged
   as a backlog candidate, not scoped yet. See `IMPLEMENTATION_BACKLOG.md`.
