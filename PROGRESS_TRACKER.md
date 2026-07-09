# Project Progress Tracker

## Project status

The refactor has moved from planning into a working starter implementation for a new architecture:
- React frontend
- ASP.NET Core backend
- PostgreSQL database
- Docker-based local development setup

## Current milestone

### Completed
- Created a new frontend project structure with React and Vite
- Created a new backend API project with ASP.NET Core
- Added a beer data model and CRUD API endpoints
- Added JWT-based authentication endpoints for login and registration
- Added EF Core with PostgreSQL support
- Added Docker Compose support for:
  - PostgreSQL database
  - backend API
  - frontend web app
- Added a mobile-first UI shell and basic pages for:
  - home
  - beer list
  - beer detail
  - add/edit beer form
  - auth page

### Verified
- Backend build succeeded with `dotnet build`
- Frontend build succeeded with `npm run build`
- Local Docker stack started successfully and exposed the app at `http://localhost:3001`
- Backend API became available at `http://localhost:5153`

## Remaining work

### Near-term
- Seed sample data for beers
- Add better protected create/edit/delete flows
- Add role-based admin behavior
- Improve mobile UX polish
- Add environment-based config for AWS deployment

### Medium-term
- Add search and filtering
- Add image support
- Add favorites or saved beers
- Add reviews and ratings
- Add deployment pipeline for AWS

## Local run instructions

### Docker
```bash
cd /Users/peco80/workspace/FinalCapstone/beer-app
docker compose up --build
```

### Manual
- Backend: `dotnet run` in the backend folder
- Frontend: `npm install && npm run dev` in the frontend folder

## Notes

This project is now in a strong transitional state: the old ASP.NET MVC app has been documented and planned, and the new architecture is now scaffolded enough to begin iterative feature development.

## Suggested next focus

1. Seed initial beer data
2. Improve authentication UX
3. Add admin-only editing workflow
4. Polish the mobile experience
5. Prepare for AWS deployment
