# Feature Map

## 1. Core product features

### Mug club progress (primary driver)
- Customer sees which of the ~200 listed beers they've had and how many remain
- Bartender looks up a customer and confirms a specific beer for them (replaces the paper sheet initials — this is the step that actually marks a beer complete)
- Milestone/reward flag when a customer completes all 200 beers (mug earned)
- Admin can view/correct confirmation history (bartender error correction)
- Data model anticipates multiple taverns/locations, even though v1 targets one

### Catalog experience
- Browse beers
- View beer details
- Search by name, brewery, or style
- Filter by category or metadata
- Sort results

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

### Administration
- Manage users
- Manage roles and permissions (admin, bartender, customer)
- Review content submissions
- Moderate records
- Correct or audit bartender beer confirmations

## 2. Phone-first user stories

- As a user, I can browse the beer catalog quickly from my phone.
- As a user, I can search for a beer without lots of typing.
- As a user, I can view the details of a beer on a small screen.
- As an authorized user, I can create or edit a beer with a simple form.
- As a customer, I can view my mug club progress (X of 200) on my phone.
- As a bartender, I can quickly look up a customer and confirm a beer for them from a phone or tablet at the bar — this needs to be fast enough to use mid-shift, not a laptop-admin-style workflow.

## 3. Laptop admin stories

- As an admin, I can see a list of beers and manage them efficiently.
- As an admin, I can review content changes in a structured dashboard.
- As an admin, I can manage users and permissions from a desktop interface.

## 4. Later-phase features

- Ratings and reviews
- Favorites and saved lists
- Recommendations
- Notifications
- Image uploads
- Analytics dashboards
- Open Brewery DB API integration — pull brewery info and images from https://www.openbrewerydb.org/ instead of (or alongside) manually entered brewery data

## 5. Suggested MVP feature priority

### Priority 1
- Beer list
- Beer details
- Create/edit/delete beer
- Authentication (customer, bartender, admin roles)
- Bartender beer-confirmation flow
- Customer mug club progress view
- Basic admin access

### Priority 2
- Search and filtering
- Better mobile form experience
- Improved admin dashboard
- Bartender-confirmation error correction/audit tools

### Priority 3
- Reviews, favorites, and social features
