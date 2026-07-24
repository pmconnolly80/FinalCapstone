# UX Discovery Worksheet — Real-World Flow Questions

A standalone, offline-friendly copy of the 20 questions from `UX_REDESIGN_BRAINSTORM.md`,
each expanded with prompts to help you think through a real, concrete answer rather than
a general impression. Answer in as much detail as you want — a specific memory of an
actual bar visit is more useful here than an abstract description of how it "should"
work. There's a blank "Your answer" space under each question; fill it in and bring it
back (or paste it into chat) whenever you're ready.

---

## Theme 1: The moment a customer opens the app

### 1.1 — When does a customer actually open this app during a visit?

Right when they walk in and order their first beer? Only once they've already had a
few and want to check progress? Does the answer change depending on whether it's their
first visit ever versus their fiftieth?

**Helpers to think through:**
- Picture an actual regular at this tavern. What's the first thing they do when they
  sit down — order, then pull out their phone? Or order, drink, and only think about
  the app later?
- Does a *first-time* customer behave differently than a *regular*? A first-timer
  might need the app explained to them by the bartender; a regular might have it open
  before they even sit down.
- Is there a moment where opening the app is the customer's idea, versus a moment
  where the bartender prompts them ("hey, are you doing the mug club thing?")?

**Your answer:**

>

---

### 1.2 — Does a customer need to be logged in to do anything useful?

Should browsing the beer list work while signed out, with login only required at the
actual confirm moment — or should the app expect you to be signed in before it's
useful for anything at all?

**Helpers to think through:**
- Think about someone who's never been to this tavern before and is just curious what
  beers they have. Should they be able to poke around without creating an account
  first?
- Is there a real scenario where someone wants to browse *before* deciding whether to
  join the mug club at all — i.e., does browsing need to work as a sales pitch for
  signing up?
- Or is that overthinking it, because in practice nobody opens this app who isn't
  already a customer sitting at the bar?

**Your answer:**

>

---

### 1.3 — Should login just... stay logged in indefinitely?

Most apps today don't make you log in every single time. If a customer logs in once,
should "landing after login" really only be a same-night, first-ever event — not
something that happens on every visit?

**Helpers to think through:**
- How long does this tavern's typical customer go between visits — days, weeks? Does
  the app need to survive that gap without asking them to log in again?
- Is there a security reason this app specifically might want to log people out more
  aggressively than a typical app (e.g., a shared/borrowed phone, a lost phone)? Or is
  that not a realistic concern for this use case?
- If someone hands their phone across the bar for the bartender to type a PIN into,
  does staying logged in create any risk you're worried about (the bartender seeing
  more than just the confirm screen, for instance)?

**Your answer:**

>

---

### 1.4 — What should the very first thing on screen actually be?

Given your answers above — is it the confirm-a-beer action immediately, or does
something else need to happen first (a welcome moment, a quick progress glance, a
prompt to pick a beer)?

**Helpers to think through:**
- If you had to describe the very first screen in one sentence to someone who's never
  seen the app, what would that sentence be?
- Does that first screen change for a brand-new customer (who has zero progress and
  might need onboarding) versus someone who's confirmed 150 beers already?

**Your answer:**

>

---

## Theme 2: Finding the beer to confirm

### 2.1 — How does "enter the beer" actually work?

Is it a search box, a list of what's currently on tap, something else? How does a
customer realistically know which beer they're drinking well enough to find it in a
list?

**Helpers to think through:**
- Picture the actual tap list or menu at this tavern. Does a customer read the beer
  name off a chalkboard, a printed menu, a tap handle, or just ask the bartender what
  they're pouring?
- If a customer only half-remembers the name ("some kind of hazy IPA"), does the app
  need to help them narrow it down, or is that the bartender's job to sort out
  verbally before the phone even comes out?
- Is scrolling through ~200 beers to find the right one realistic in a noisy bar with
  a slightly-drunk customer, or does this need to be closer to picking from "what's on
  tap right now" (a much shorter list)?

**Your answer:**

>

---

### 2.2 — Where does confirming actually start from?

Is confirming reachable directly from a beer's row in a list (tap the beer → confirm),
or is "enter the beer" a dedicated, separate, single-purpose screen that's not the same
as browsing/searching the whole catalog?

**Helpers to think through:**
- Think about the physical handoff moment: customer's beer arrives, they need to get
  to the confirm screen fast. Is browsing the whole catalog first (for taps, styles,
  brewery info) part of that same moment, or does that happen at a totally different
  time (before ordering, out of curiosity)?
- Would combining "browse/search" and "confirm" onto one screen make the confirm
  moment slower (more to tap through) or would splitting them into two separate places
  actually add an extra step instead of removing one?

**Your answer:**

>

---

### 2.3 — What about ordering more than one beer at once?

If a customer orders a flight, or orders for a friend, does the flow need to support
confirming several beers back-to-back without extra friction, or is one-at-a-time
genuinely fine?

**Helpers to think through:**
- Does this tavern actually serve flights or multi-beer rounds often enough for this to
  matter, or is "one beer confirmed per phone-hand-off" close enough to how it happens
  in practice?
- If someone orders for a friend, whose phone gets used — the orderer's, or does the
  friend need to hand over their own phone separately? Does the app need to know the
  difference?

**Your answer:**

>

---

### 2.4 — What happens with a beer that isn't on the list at all?

If a customer is drinking something that isn't in the tavern's ~200-beer catalog
(a seasonal one-off, a guest tap), what should happen?

**Helpers to think through:**
- Does this actually happen at this tavern — one-off or rotating taps that aren't
  formally added to the list yet? How often?
- Is that the admin's problem to solve (add the beer to the catalog first) before a
  customer can confirm it, or does the customer-facing flow need its own way to handle
  "this beer isn't listed yet"?

**Your answer:**

>

---

## Theme 3: The PIN moment itself — the part that has to be fast

### 3.1 — Walk through the handoff, second by second.

Customer finishes getting their beer — then what, literally? Does the customer
proactively hand their phone across the bar, or does the bartender ask for it? Who
initiates, and what do they say to each other?

**Helpers to think through:**
- Try to picture (or remember) an actual moment like this. What are the exact words
  and physical motions? "Here, can you type your number in?" — handing the phone
  across — bartender types — hands it back?
- Is there a natural point in serving a beer where this fits in without slowing the
  bartender down (right when they set the glass down, while making change, etc.), or
  does it always feel like an extra step no matter when it happens?

**Your answer:**

>

---

### 3.2 — Is a 6–8 digit PIN actually fast enough for a busy bartender?

As of the current build, an admin can set a bartender's PIN to be anywhere from 6 to
8 digits. Is that realistic to type quickly and accurately mid-rush, or would something
shorter — or a completely different way of identifying the bartender — feel less like
a chore?

**Helpers to think through:**
- Picture a bartender's hands on a Friday night — wet, cold from ice, maybe wearing
  something on their fingers, moving fast. How many digits feels "quick" to type on an
  unfamiliar phone's on-screen keyboard, in that state?
- Is there a real bartender at this tavern you could actually ask "would this feel like
  a hassle to you"? Sometimes the fastest way to answer this section is to ask the
  person who'd actually do it every night.
- Would a 4-digit PIN feel meaningfully faster in practice, or is 6-8 digits not
  actually the bottleneck (i.e., is the real friction something else, like the app just
  taking too many taps to get to the PIN screen in the first place)?

**Your answer:**

>

---

### 3.3 — Should the PIN auto-submit the instant it's long enough?

Right now, does (or should) typing the right number of digits submit automatically, or
does the bartender still have to tap a separate "Confirm" button after typing the PIN?

**Helpers to think through:**
- If PINs can be different lengths per bartender (6 vs. 8 digits), does auto-submit
  actually work reliably, or does a bartender risk it firing early / not firing until
  they add one more digit than needed?
- Is a manual "Confirm" tap actually a meaningful source of friction, or is it a good
  safety net against an accidental extra digit submitting the wrong thing?

**Your answer:**

>

---

### 3.4 — Is the one-device rule (customer's phone only) still right?

The current design has no bartender-facing screen or device at all — everything happens
on the customer's phone, with the bartender only ever typing their own PIN into it. Now
that there's been real usage, does that still feel right, or has anything about it felt
awkward in practice?

**Helpers to think through:**
- Has a bartender ever seemed reluctant, confused, or slowed down by having to use a
  customer's unfamiliar phone (different screen size, unfamiliar app layout, a locked
  screen they have to wait for the customer to unlock)?
- Is there a real moment where a bartender-side device (even something minimal, like a
  shared tablet behind the bar) would have made things faster instead of slower — or is
  that exactly the complexity the one-device rule was trying to avoid, and it's still
  worth avoiding?
- Note: this was a deliberate decision made in `TECHNICAL_ARCHITECTURE_PLAN.md` — not
  wrong by default, just worth re-checking against actual experience now that there is
  some.

**Your answer:**

>

---

## Theme 4: What "everything else" contains, and where it goes

### 4.1 — Which screens does a customer actually want while standing at the bar?

Of the things this app can show — progress toward the mug, the full beer list, rating a
beer just confirmed, a want list, account/PIN settings — which of those does a customer
plausibly reach for *in the moment*, at the bar, versus only caring about later at home?

**Helpers to think through:**
- If you imagine someone standing at the bar with a drink in one hand and their phone
  in the other, what are they actually trying to do with the app in that specific
  moment? Probably not adjusting account settings.
- Is "checking progress" itself an at-the-bar thing (bragging to a friend, deciding
  whether to order one more) or a some-other-time thing (checking in from home the next
  day)?

**Your answer:**

>

---

### 4.2 — Does that suggest fewer tabs, not just different ones?

Today's bottom tab bar has four tabs: Home, Beers, My Progress, Account. Given your
answer above, does the real at-the-bar-vs-later split suggest collapsing some of these
together, or is four tabs actually fine and it's just which *one* is the default landing
tab that needs to change?

**Helpers to think through:**
- If you had to cut the tab bar down to two tabs, which two would survive, and what
  would you do with the other two (bury them under one of the survivors, like the
  existing Account hub already does for some things)?
- Is there a difference between what a *customer* needs easy access to versus what an
  *admin* needs — should admin-only links even be in the same tab bar a customer sees,
  or does that not actually cause any real confusion in practice?

**Your answer:**

>

---

### 4.3 — Is there a gold-standard app you're already comparing this to?

A coffee loyalty app, Untappd, something else — naming a specific app you already think
feels right gives a concrete target instead of an abstract "make it seamless."

**Helpers to think through:**
- Next time you open that app (whatever it is), notice exactly what happens in the
  first three seconds after you launch it. What's on screen? How many taps to the thing
  you actually came to do?
- Is it the *speed* of that app you're drawn to, or something else about it (how it
  looks, how it rewards you, how little it asks of you)? Naming what specifically about
  it works will matter more than just naming the app.

**Your answer:**

>

---

### 4.4 — Where do ratings and the want list fit into this?

Sprint 8 already shipped a basic "how was it" rating prompt, and a want list is coming
in Sprint 9. Do either of those belong anywhere near the fast confirm flow, or are they
both squarely "later, not at the bar" features?

**Helpers to think through:**
- Right after confirming a beer, is asking "how was it?" a nice extra moment, or does
  it add friction to a flow that's supposed to be as fast as possible? Would a customer
  actually want to answer that while still at the bar, or is that something better
  offered later (next time they open the app)?
- Does a want list ("beers I want to try") feel like something a customer builds while
  actively browsing/planning at home, or something they'd add to in-the-moment at the
  bar too (seeing something on tap they don't want right now but want to remember)?

**Your answer:**

>

---

## Theme 5: What happens when it goes wrong, in a real bar

### 5.1 — Bad signal at the exact moment of confirming.

There's already a distinct "no signal" message for this. Does the *recovery* need to be
faster or different in a real bar setting, or is the message alone enough?

**Helpers to think through:**
- Does this tavern actually have known dead spots for phone signal or wifi — near the
  bar specifically, or elsewhere? How often has this actually come up?
- If it happens, what does the bartender or customer do right now, physically? Wait and
  retry? Give up and try again in a minute? Does the app need to make that retry
  easier, or is "just try again" already fine?

**Your answer:**

>

---

### 5.2 — A bartender fat-fingers the PIN a couple times.

Busy hands, dim lighting, a phone at an odd angle — today's default locks a PIN out for
15 minutes after 5 wrong attempts. Is that the right real-world tradeoff, or does it
need rethinking?

**Helpers to think through:**
- How often do you think an honest mistake (mistyped PIN) would actually rack up 5
  wrong attempts in a row, versus a bartender noticing after 1-2 tries and stopping to
  double check?
- If a bartender does get locked out for 15 minutes on a busy Friday, what actually
  happens next — do they just stop using the app for that stretch, ask another
  bartender to cover, or call over an admin? Is 15 minutes an eternity in that
  situation, or a non-issue because it rarely triggers by accident?

**Your answer:**

>

---

### 5.3 — What's the actual worst-case failure you're trying to avoid?

A customer giving up mid-flow and not confirming at all? A bartender refusing to bother
with it because it's slower than the old paper sheet? Something else entirely?

**Helpers to think through:**
- If you had to bet on the single most likely reason this app fails to get adopted at
  this specific tavern, what would you bet on? Be specific — not "it's too complicated"
  in the abstract, but the actual moment where someone would give up or refuse.
- Is that failure mode about speed, about confusion, about it just being one more thing
  to remember to do, or about trust (a bartender not wanting to touch a customer's
  phone, for instance)?

**Your answer:**

>

---

### 5.4 — Is there a failure mode nobody's mentioned yet?

Anything about real bar conditions — noise, crowding, drunk customers, a bartender
juggling six things at once, a phone with a cracked screen or dead battery — that hasn't
come up in the questions above but feels important?

**Helpers to think through:**
- Think about the worst possible Friday night at this tavern — packed, loud, everyone
  wanting a drink at once. Does the app survive that night, or does it quietly get
  abandoned until things calm down?
- Is there anything about *this specific tavern* (its layout, its regulars, its typical
  crowd) that makes it different from a generic bar in ways the app should account for?

**Your answer:**

>

---

*Once you've worked through these, bring your answers back and we'll fold them into
`UX_REDESIGN_BRAINSTORM.md`'s decisions log and figure out what actually changes.*
