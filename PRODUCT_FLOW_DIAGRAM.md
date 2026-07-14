# Product Flow Diagram

## 1. Mobile customer flow (find → read → confirm)

```mermaid
flowchart TD
    A[Open app on phone] --> B[Home: X of 200 + search bar]
    B --> C[Search or filter the list<br/>in-stock by default, had/not-had chips]
    C --> D[Open beer details<br/>style + description + OBDB brewery card]
    D --> E{Drinking it now?}
    E -->|Yes| F[Tap Confirm with bartender]
    E -->|No| G[Save to journal / keep browsing]
    F --> H[Full-screen PIN pad<br/>beer + customer name shown large]
    H --> I[Hand phone to bartender]
```

## 2. Confirmation flow (one device — the customer's phone)

```mermaid
flowchart TD
    A[Customer orders a beer] --> B[Customer finds beer, taps<br/>Confirm with bartender]
    B --> C[PIN pad fills the customer's screen]
    C --> D[Phone handed across the bar]
    D --> E[Bartender keys personal 6-digit PIN]
    E --> F{PIN valid?<br/>server-side check: role, active, lockout}
    F -->|No| G[Masked failure, retry<br/>repeated failures → cooldown lockout]
    F -->|Yes| H[Record: customer from session + beer<br/>+ bartender resolved from PIN + timestamp]
    H --> I{Customer now at 200?}
    I -->|Yes| J[MUG EARNED — milestone screen,<br/>opt-in feed shout-out, owner flag]
    I -->|No| K[Phone handed back showing updated count<br/>milestone badge if 25/50/100/150 crossed]
```

## 3. Laptop admin flow

```mermaid
flowchart TD
    A[Open admin dashboard on laptop] --> B{Anomaly panel item?<br/>bulk beer-adds, velocity spikes}
    B -->|Yes| C[Review flagged activity]
    C --> D[Correct any record — beer, confirmation,<br/>account, social content — with reason note]
    D --> E[Audit log records who/what/when/why]
    B -->|No| F[Routine upkeep]
    F --> G[Add delivery beers<br/>OBDB brewery autocomplete]
    G --> H[Set availability: on tap /<br/>out of stock / retired]
    F --> I[User & role management,<br/>bartender PIN issue/reset/deactivate]
```

## 4. Owner push-notification flow

```mermaid
flowchart TD
    A[Owner opens notification composer] --> B[Pick audience: all / active /<br/>lapsed / hasn't-had-beer-X]
    B --> C[Write message, preview]
    C --> D[Send now or schedule]
    D --> E[Background job fans out to<br/>consenting members' push subscriptions]
    E --> F[Frequency caps + quiet hours enforced]
    F --> G[Delivery counts back on dashboard]
```

## 5. Account flow

```mermaid
flowchart TD
    A[Open app] --> B{Signed in?}
    B -->|No| C[Continue with Google / Facebook / Apple<br/>or email + password]
    C --> D[Link-or-create member on verified email<br/>+ marketing-consent checkbox]
    D --> E[App issues its own JWT with role claims]
    B -->|Yes| F[Access protected actions]
    E --> F
    F --> G[Manage account, linked providers,<br/>social opt-in / display name]
```

## 6. High-level system flow

```mermaid
flowchart LR
    U[Member on phone / installed PWA] --> UI[Web app + service worker]
    L[Owner or admin on laptop] --> UI
    UI --> API[Backend API]
    API --> DB[(PostgreSQL)]
    API --> AUTH[Identity + social sign-in providers]
    API --> OBDB[Open Brewery DB<br/>server-side cached]
    API --> PUSH[Web Push service]
    PUSH --> U
```
