# UX Redesign Brainstorm — Real-World Flow Discovery

**STATUS: OPEN. No implementation work happens against this app until the questions
below are answered and a direction is agreed.** This is a discovery/planning document,
not a backlog — nothing here becomes a sprint issue until it's resolved into a decision.

An expanded, offline-friendly copy of the question set below — each question with
brainstorming helpers/prompts and space to write out full answers — lives in
`UX_DISCOVERY_QUESTIONS_WORKSHEET.md`. Work through that one; its answers feed the
"Answers / decisions log" section at the bottom of this doc.

## Why this started (2026-07-24)

Live use of the current build surfaced two different classes of problem:

1. **A real bug.** `AuthPage.jsx` never navigates away after a successful login or
   register — it dispatches `AUTH_CHANGED_EVENT` (which updates the bottom tab bar's
   visibility) but never redirects off `/auth`. Result: after logging in, the login
   form is still on screen underneath, while the tab bar has now appeared around it.
   Root cause identified, fix is trivial (redirect to `/` on success) — **intentionally
   not applied yet**, because the right redirect target depends on the answer to the
   bigger question below.

2. **A direction problem, stated by the user:** *"we want this to be a pretty seamless
   application for the customer and the bartender confirmation should work very quickly
   with very little needed on their end other than entering their number... after they
   login it should be on the screen to enter the beer. everything else needs to be on
   another tab."* Plus: *"if it is cumbersome it will not be used."*

That second point reframes more than the login redirect — it questions what the
landing screen after login even is, and by extension what today's bottom-tab-bar /
Account-hub structure (Sprint 6, #67/#82) assumed about priority. Patching the login
bug without resolving that first would mean guessing at a redirect target and
possibly redoing it once the real direction is settled.

## Also raised, parked for later (not blocking, but noted)

- **Mirroring Open Brewery DB into our own AWS-hosted database**, once the app itself
  is deployed to AWS, instead of calling the external API live. This is a data/infra
  decision independent of the UX direction below — revisit at AWS deployment time
  (`infra/aws-architecture.md` is the current placeholder for that work; no actual IaC
  exists yet per `CLAUDE.md`).
- User noted they have **other items in mind** not yet raised. Add them to this doc as
  they come up rather than losing them in chat scrollback.

## Ground rule for this discovery phase

The goal is to describe **how this actually plays out at the bar, step by step, in real
time** — not to jump straight to screen layouts. A flow that sounds reasonable in the
abstract can still be cumbersome in practice (a bartender mid-rush, a customer who's had
a few drinks, bad bar wifi, a phone screen that locks after 30 seconds). Answering the
questions below in terms of *what actually happens physically* is what will keep the
next redesign from repeating that mistake.

---

## Questions to work through

### 1. The moment a customer opens the app

- When does a customer actually open this app during a visit — right when they walk in
  and order their first beer, or only once they've already been drinking a while and
  want to confirm what they've had? Does that change what the very first screen should
  show?
- Does a customer need to be logged in to do *anything* useful, or should browsing the
  beer list work while signed out, with login only required at the confirm moment?
- Once a customer has logged in once, should the app just... stay logged in on their
  phone indefinitely (like most apps today), so "landing after login" is really a
  same-night event, not something that happens every visit?

### 2. Finding the beer to confirm

- Is "enter the beer" a search box, a tap-list of what's currently on tap, or something
  else? How does a customer realistically know which beer they're drinking well enough
  to find it in a list — do they read it off the tap handle, the menu, ask the
  bartender?
- Should confirming be reachable directly from a beer's row in a list (tap the beer →
  confirm), or does "enter the beer" mean a dedicated single-purpose screen that's
  separate from browsing/searching the catalog?
- If a customer orders a flight or several beers in one round, does the flow need to
  support confirming more than one beer back-to-back without extra taps, or is
  one-at-a-time fine?

### 3. The PIN moment itself — the part that has to be fast

- Walk through this literally, second by second: customer finishes/gets their beer,
  then what? Does the customer hand their phone across the bar unprompted, or does the
  bartender ask for it? Who initiates?
- Today's PIN is 6–8 digits (as of Sprint 8, admin can choose the length per bartender).
  Is that actually fast enough for a bartender mid-rush, or would something shorter —
  or a different resolve-bartender mechanism entirely — feel less like a chore?
- Should the PIN field auto-submit the instant the right number of digits is typed
  (no separate "Confirm" tap needed), to shave off one interaction?
- Is the **one-device rule** (customer's phone only, no bartender device, decided in
  `TECHNICAL_ARCHITECTURE_PLAN.md` §4.1) still the right call now that there's been real
  usage, or has anything in practice made a bartender-side device feel more necessary
  than it seemed at the time?

### 4. What "everything else" actually contains, and where it goes

- Which of these does a customer plausibly want *while standing at the bar*, versus
  only at home afterward: checking progress toward the mug, browsing the full beer
  list, rating a beer just confirmed, checking their want list, account/PIN settings?
- Does splitting "at-the-bar" actions from "later, at home" actions suggest a different
  tab structure than today's four-tab bar (Home / Beers / My Progress / Account) — or
  does it suggest fewer tabs, with more collapsed under one?
- Is there a specific app you already think of as the gold standard for "this is how
  fast/seamless it should feel" (a coffee loyalty app, Untappd, something else)? Naming
  one gives a concrete target to compare against instead of an abstract "seamless."

### 5. What happens when it goes wrong, in a real bar

- Bad phone signal at the exact moment of confirming — today that's a distinct error
  message (#69/#71). Does the *recovery* need to be faster/different in a real bar
  setting, or is the message alone enough?
- A bartender fat-fingers the PIN two or three times mid-rush (busy hands, dim
  lighting, a phone screen at an angle) — is a 15-minute lockout (today's default)
  the right real-world tradeoff, or does that need rethinking given how often mis-taps
  might actually happen versus real brute-force attempts?
- What's the actual worst-case failure you're trying to avoid — a customer giving up
  mid-flow and not confirming at all, a bartender refusing to bother with it because
  it's slower than initialing a paper sheet, or something else? Naming the specific
  failure mode sharpens which of the above questions matter most.

---

## Answers / decisions log

*(Nothing decided yet — fill in as the conversation progresses. Once a section here is
resolved, it should get folded into `PROJECT_PLAN.md`/`MOBILE_FIRST_PRODUCT_OUTLINE.md`/
`TECHNICAL_ARCHITECTURE_PLAN.md` as appropriate, and this doc can note that it moved
rather than duplicating it.)*
