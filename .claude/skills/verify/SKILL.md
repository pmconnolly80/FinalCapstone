---
name: verify
description: Build, run, and drive the beer-app stack (Postgres + ASP.NET Core API + React web) to verify a change end-to-end at its HTTP/UI surface.
---

# Verify beer-app changes

## Launch

```bash
cd beer-app
docker compose up -d --build api web     # db keeps its volume; rebuild picks up code changes
docker compose logs api | tail           # wait for "Now listening"; migrations run at startup
```

- API: `http://localhost:5153` (Swagger at `/swagger`) · Web: `http://localhost:3001`
  (Vite dev server, but `docker-compose.yml` has no volume mount for `web` — the
  Dockerfile `COPY`s source once at build time, so frontend edits need
  `docker compose up -d --build web` to show up, same as backend changes)
- DB: `docker compose exec db psql -U beeruser -d beerdb -tc '<sql>'` — check
  `"__EFMigrationsHistory"` to confirm a new migration applied.

## Local (no Docker) backend

Only the .NET **10** SDK is on PATH; the projects target **net8.0**. A .NET 8 SDK is
installed at `~/.dotnet8`. Run backend things as:

```bash
DOTNET_ROOT="$HOME/.dotnet8" PATH="$HOME/.dotnet8:$PATH" dotnet test beer-app/BeerApi.Tests/BeerApi.Tests.csproj
```

Do NOT use `DOTNET_ROLL_FORWARD=LatestMajor` for tests — the net8 TestHost on the
.NET 10 runtime hits a System.Text.Json PipeWriter incompatibility and every
integration test 500s with "PipeWriter ... does not implement UnflushedBytes".
`dotnet ef` (tool 8.0.11) is the exception: roll-forward is fine for migrations
(`DOTNET_ROLL_FORWARD=LatestMajor dotnet ef migrations add <Name>` in `beer-app/backend`).

## Drive the core loop (curl)

```bash
API=http://localhost:5153
TOKEN=$(curl -s -X POST $API/api/auth/register -H 'Content-Type: application/json' \
  -d '{"email":"<unique>@example.com","password":"Passw0rd!"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["token"])')
curl -s $API/api/beers                                   # ids of seeded beers
curl -s -X POST $API/api/confirmations -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' -d '{"beerId":2,"pin":"123456"}'   # 201
curl -s $API/api/me/progress -H "Authorization: Bearer $TOKEN"           # progress
```

- Seeded dev bartender PIN: `123456` (user `bartender@example.com` / `Bartender1!`).
- Registration requires a fresh email per run — the db volume persists across runs.
- Good probes: no token (401), wrong PIN `000000` (401 generic), malformed PIN (400),
  duplicate confirm (409), unknown beerId (404).

## Gotchas

- The whole compose stack may already be up from a previous session — `--build` is what
  gets your backend changes in; a plain `up -d` will happily keep the stale image.
- No browser automation is set up; UI verification = RTL tests + checking the dev server
  serves the module (`curl -s http://localhost:3001/src/<path>.jsx`) + verifying API
  response field casing (camelCase) matches what components read.
