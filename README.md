# Marketplace Copilot

AI-powered deal desk for cloud marketplace sellers — hackathon demo.

**Full demo guide:** see **[HACKATHON-DEMO.md](./HACKATHON-DEMO.md)** (features, script for judges, API reference, troubleshooting).

## Quick start

```powershell
.\start-demo.ps1
```

Open **http://localhost:4200** → click **Try Demo Login** (`demo@marketplace.com` / `demo123`).

## What's included

| Area | Status |
|------|--------|
| Auth (demo login, signup UI, OAuth stubs) | ✅ |
| Home dashboard (KPIs, tasks, reminders, AI chat) | ✅ |
| Deals list (search, created date, pagination) | ✅ |
| Deal workflow (create → products → pricing → notes) | ✅ |
| Pricing calculator (duration, FPS, pro-rate, live preview) | ✅ |
| Meeting notes + AI extract (standard + dynamic fields) | ✅ |
| Tracking tabs (sessions, actions, reminders, audit log) | ✅ |
| Deal overview + change history | ✅ |

**Stack:** Angular + .NET 9 layered solution (Api · Services · Data · Entities) · JSON file store · rule-based AI (offline demo)

## Manual run

```powershell
# Terminal 1 — backend (or: dotnet run --project MarketplaceCopilot.Api)
cd MarketplaceCopilot.Api && dotnet run

# Terminal 2 — frontend
cd frontend && npm start
```

> The backend is a multi-project solution (`MarketplaceCopilot.sln`). Build everything with `dotnet build` at the repo root. See [HACKATHON-DEMO.md](./HACKATHON-DEMO.md#project-structure) for the layer layout.

API: http://localhost:5280 · UI: http://localhost:4200
