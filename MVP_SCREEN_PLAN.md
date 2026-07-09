# MVP Screen Plan

## 1. Mobile screens

### Home / Dashboard
- Welcome area
- Quick links to browse beers
- Search entry point
- Recently viewed or featured beers

### Beer List
- Search bar
- Filter controls
- Scrollable list of beers
- Each item shows name, brewery, and style

### Beer Detail
- Beer name
- Brewery
- Style
- Description or notes
- Edit action for authorized users

### Login / Register
- Email and password form
- External login options if desired
- Clear error states

### Create / Edit Beer
- Simple form for name, brewery, style
- Validation and save action
- Cancel or go back

### My Progress (customer)
- Progress toward 200 (count and percent)
- List of confirmed beers with confirmation date
- List of remaining beers
- "Mug earned" state once 200 is reached

### Confirm Beer (bartender, phone/tablet at the bar)
- Search or select customer
- Search or select beer
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
- Add, edit, delete actions
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

1. Home / browse screen
2. Beer detail screen
3. Create / edit beer form
4. Login / register
5. Bartender: confirm beer for customer
6. Customer: my progress
7. Admin dashboard
8. User management
