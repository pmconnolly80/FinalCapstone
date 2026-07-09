# Mobile-First Product Outline

## 1. Product goal

Build a mobile-first beer application that is easy to use on a phone, while also supporting management workflows on a laptop.

Concretely: digitize a tavern's paper mug-club sheet, where a bartender initials next to a beer once a customer has had it. The app needs a fast, at-the-bar bartender flow (phone/tablet, not laptop) to confirm a beer for a customer, plus a customer-facing progress view toward the 200-beer goal.

## 2. Core usage pattern

- Primary customer interaction happens on a phone (browsing, viewing progress)
- Bartender confirmation happens on a phone or tablet at the bar — it needs to be quick enough to use mid-shift, closer to a point-of-sale interaction than a content-management one
- Back-office management and administration happen on a laptop
- The product should feel fast and lightweight on mobile
- The same core data and business rules should support both experiences

## 3. Primary user experience on mobile

### Goals
- Quick access to core content
- Minimal friction for browsing and interacting
- Touch-friendly layouts
- Fast loading and simple navigation

### Mobile-first screens
- Home / dashboard
- Browse beer catalog
- Beer detail page
- Search and filter
- Quick create or edit actions
- Account / profile
- My mug club progress (X of 200, remaining beers)
- Bartender: customer lookup + confirm beer (separate fast-path flow, not part of admin)

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
- Browse beers
- Search beers
- View beer details
- Log in / sign up
- Basic create/edit/delete for authorized users
- Customer: view mug club progress toward 200
- Bartender: look up a customer and confirm a beer for them

### Must-have laptop flows
- Admin dashboard
- Manage beer records
- Review and approve content changes

## 8. Future expansion opportunities

- Favorites and saved beers
- Ratings and reviews
- Notifications
- Advanced search and recommendations
- Native mobile app later if demand increases
