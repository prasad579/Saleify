# Marketplace Copilot — Hackathon Demo Guide

AI-powered deal desk for cloud marketplace sellers. This document covers everything built so far, how to run the demo, and a suggested walkthrough for judges.

---

## Elevator pitch (30 seconds)

**Marketplace Copilot** helps deal desk teams move faster from opportunity to private offer. Sellers create deals, attach products, configure pricing with live calculations, capture meeting notes with AI extraction, and track action items, reminders, and change history — all in one workspace with an embedded AI assistant.

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Frontend | **Angular** (standalone components, modern routing) |
| Backend | **.NET 9 Web API** |
| Data | JSON file store (`MarketplaceCopilot.Api/data/deals.json`) — demo-friendly, no DB setup |
| AI | Rule-based **AiService** (works offline; upgradeable to OpenAI/Azure via `appsettings.json`) |
| Auth | Cookie/session + JWT token; demo account; optional Google/Microsoft OAuth stubs |

---

## Quick start

### Option A — One command (recommended)

From the project root:

```powershell
.\start-demo.ps1
```

This opens two terminals (API + Angular) and prints login details.

### Option B — Manual

**Terminal 1 — Backend**

```powershell
cd MarketplaceCopilot.Api
dotnet run
```

Wait for: `Now listening on: http://localhost:5280`

**Terminal 2 — Frontend**

```powershell
cd frontend
npm start
```

Open: **http://localhost:4200**

### Demo login

| Method | Details |
|--------|---------|
| **Try Demo Login** button | One-click on sign-in page |
| Email / password | `demo@marketplace.com` / `demo123` |
| Dev mode | Any email/password works when API is running |

> If the API is offline, the UI still loads but data saves will fail. The login page shows a banner when the backend is unreachable.

---

## Screens & routes

| Route | Screen | Status |
|-------|--------|--------|
| `/login` | Sign in (+ demo login, OAuth placeholders) | ✅ |
| `/signup` | Sign up + email verification flow (UI) | ✅ |
| `/home` | Dashboard — KPIs, open deals, tasks, reminders, AI chat | ✅ |
| `/deals` | Deals list with search, sort, pagination | ✅ |
| `/deals/new` | Create deal | ✅ |
| `/deals/:id` | **Deal overview** — snapshot + tracking tabs | ✅ |
| `/deals/:id/edit` | Edit deal details | ✅ |
| `/deals/:id/products` | Select products | ✅ |
| `/deals/:id/pricing` | Configure pricing (live calculator) | ✅ |
| `/deals/:id/meeting-notes` | Meeting notes + AI extract + full tracking | ✅ |

**Workflow stepper:** Deal Info → Products → Pricing → Meeting Notes → Approvals (approvals screen planned)

---

## Features built (detailed)

### 1. Authentication & onboarding

- Email/password login with auth guard on protected routes
- **Try Demo Login** for instant hackathon demos
- Sign-up flow with verify-email, awaiting-role, and access-granted screens (UI wired)
- Google / Microsoft OAuth endpoints (configurable via `appsettings.json`)
- Frontend proxy to API (`/api` → `localhost:5280`)

### 2. Home dashboard

- KPI cards: open deals, pending approvals, offers submitted, pipeline value
- **My Open Deals** table — sort by created date, pagination (5/page)
- **My Tasks** and **Reminders** tables with pagination
- **AI Assistant** panel with recommendations + free-text Copilot chat
- View / Continue actions per deal (smart routing to current workflow step)

### 3. Deals list (reference list pattern)

- Stats row: total, draft, in progress, submitted, accepted
- **Search** across deal ID, name, customer, marketplace, stage, owner
- **Created date** column
- **Sort** newest / oldest first
- **Pagination** (10 deals per page)
- **View** → deal overview | **Continue** → next workflow step

### 4. Deal creation & editing

- Full deal form: customer, contact, marketplace, deal type, expected value, close date, etc.
- Persisted to backend with **change history** logging
- Edit existing deals via `/deals/:id/edit`

### 5. Product selection

- Browse product catalog from API
- Attach products to deal
- Step navigation back/forward through workflow

### 6. Pricing calculator (advanced)

- Offer type, contract start/end date pickers
- Duration in **days / months / years** with bidirectional sync to dates
- Pricing methods: discount-based vs absolute contract price
- **Same discount** vs **per-year discount** (years only)
- Pro-rate toggle
- **Flexible payment schedule (FPS)** — installment count + schedule
- Live preview via API (`POST /api/deals/{id}/pricing/preview`)
- Persist pricing (`POST /api/deals/{id}/pricing`)
- Yearly breakdown + installment schedule in UI
- Back navigation to products and deal edit

### 7. Meeting notes & AI extraction

- Add new discussion: optional session title + raw notes textarea
- **Extract Insights** — AI parses notes into:
  - **Standard template fields** (contract duration, discount, payment model, legal review, customer interest)
  - **Dynamic fields** — extra topics detected from notes (not fixed schema)
  - **Suggested action items** with ISO due dates where possible
- **Save session** — appends to meeting history (multi-session support)
- Draft summary preview before save
- Last meeting snapshot card on overview and meeting notes pages

### 8. Deal tracking tabs (core differentiator)

Shared tabbed panel on **Deal overview** (read-only) and **Meeting notes** (editable):

| Tab | Features |
|-----|----------|
| **Meeting notes** | All saved sessions (not just latest); expand row for full notes + template/dynamic fields; pagination |
| **Action items** | Add/edit/delete; optional link to discussion; filter by session; pagination |
| **Reminders** | Add/edit/delete; optional session link; filter; pagination |
| **Change history** | Audit log — category filter, search, pagination |

- Tab badges show counts (e.g. 3 reminders)
- Active tab styling matches design reference (icons, underline, badges)
- Overview panel links to meeting notes for editing

### 9. Change history / audit log

- Automatic logging on: deal create/update, products, pricing, meeting notes
- Categories: Deal, Products, Pricing, Meeting Notes
- Filterable, searchable, paginated in Change history tab
- API: `GET /api/deals/{id}/history`

### 10. AI Copilot chat

- Home dashboard chat box
- API: `POST /api/ai/chat`
- Answers context-aware questions (approvals, deals, tasks) using demo rules

---

## Suggested demo script (~8–10 minutes)

### 1. Login (30 sec)

1. Open http://localhost:4200
2. Click **Try Demo Login**
3. *Talking point:* “Single workspace for marketplace deal desk — no switching tools.”

### 2. Home dashboard (1 min)

1. Point out KPIs and AI recommendations
2. Show open deals sorted by created date with pagination
3. Ask Copilot: *“What approvals are pending?”*
4. *Talking point:* “AI surfaces what matters today; deals are sortable and paginated like enterprise list views.”

### 3. Deals list (1 min)

1. Go to **Deals** in nav
2. Search for a customer name
3. Show created date, stats, pagination
4. *Talking point:* “This list pattern is our template for tasks, reminders, and pipeline views.”

### 4. Create or continue a deal (2 min)

**Option A — New deal**

1. **+ New Deal** → fill customer, marketplace, value → Create
2. **Select Products** → pick 1–2 products → Continue
3. **Configure Pricing** → change discount % → show live totals updating
4. Toggle duration (years/months) or contract dates
5. Save pricing → Continue to Meeting Notes

**Option B — Existing deal**

1. Click **Continue** on a deal in progress
2. Jump to pricing or meeting notes as appropriate

### 5. Meeting notes + AI (2 min)

1. Paste sample notes, e.g.:

   ```
   Client wants 3-year contract at 20% discount. Legal review required before we send the offer.
   They asked for a follow-up call next Friday. Also interested in flexible monthly payments.
   ```

2. Click **Extract Insights**
3. Show draft summary — standard + **Dynamic** fields
4. Show suggested action items linked to current draft
5. **Save session**
6. *Talking point:* “AI extracts structured data and follow-ups; fields aren’t fixed — dynamic columns appear based on the conversation.”

### 6. Tracking tabs (2 min)

1. Scroll to tabbed panel
2. **Meeting notes** tab — show multiple sessions, expand one
3. **Action items** — filter by discussion, show optional session link
4. **Reminders** tab
5. **Change history** — filter by Pricing or Meeting Notes
6. Go to **Deal overview** (`/deals/:id`) — same tabs in read-only mode
7. *Talking point:* “Full audit trail plus meeting follow-up in one place — linked to specific discussions when needed.”

### 7. Close (30 sec)

- Recap: faster deal cycles, AI-assisted notes, unified tracking
- Mention: real OpenAI plug-in ready; SQL database next step

---

## API reference (main endpoints)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/health` | Health check |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/signup` | Register |
| GET | `/api/dashboard` | Home dashboard data |
| GET | `/api/deals` | List deals |
| GET | `/api/deals/stats` | Deal statistics |
| GET | `/api/deals/{id}` | Deal detail |
| GET | `/api/deals/{id}/history` | Change history |
| POST | `/api/deals` | Create deal |
| PUT | `/api/deals/{id}` | Update deal |
| POST | `/api/deals/{id}/products` | Set products |
| POST | `/api/deals/{id}/pricing/preview` | Live pricing calc |
| POST | `/api/deals/{id}/pricing` | Save pricing |
| POST | `/api/deals/{id}/meeting-notes` | Save sessions, actions, reminders |
| POST | `/api/ai/extract-insights` | AI note extraction |
| POST | `/api/ai/chat` | Copilot chat |
| GET | `/api/products` | Product catalog |
| GET | `/api/lookups` | Dropdown lookups |

---

## Project structure

```
Marketplace-Copilot/
├── start-demo.ps1                       # One-click demo launcher
├── HACKATHON-DEMO.md                    # This file
├── MarketplaceCopilot.sln               # Layered .NET solution
├── MarketplaceCopilot.Api/              # Presentation layer (REST controllers + Program.cs)
│   ├── Controllers/                     # REST API
│   └── data/deals.json                  # Demo data (read from host content root)
├── MarketplaceCopilot.Services/         # Business logic (concrete services)
│   ├── AiService.cs                     # Insight extraction + chat
│   ├── PricingService.cs                # Pricing calculations
│   ├── MeetingNotesService.cs
│   ├── DealHistoryService.cs
│   └── ApprovalService.cs
├── MarketplaceCopilot.Services.Contracts/  # Service interfaces (IDealService, ...)
├── MarketplaceCopilot.Data/             # Data layer (DataStore JSON persistence, UserStore)
├── MarketplaceCopilot.Entities/         # Domain models + DTOs
│   ├── Models/                          # Deal, Pricing, MeetingNotes, etc.
│   └── Dtos/                            # DealDetailDto, ApprovalSummary, ...
├── Repository.Pattern/                  # Generic repository base (IObjectState, IRepository, UnitOfWork)
├── Service.Pattern/                     # Generic service base (IService, Service<T>)
└── frontend/src/app/
    ├── core/                            # App-wide singletons
    │   ├── services/api.service.ts      #   (@core/*)
    │   └── guards/auth.guard.ts
    ├── shared/                          # Reusable building blocks (@shared/*)
    │   ├── components/                  #   deal-tracking-panel, last-meeting-snapshot, ...
    │   ├── utils/                       #   pricing, meeting-notes, pagination
    │   └── data/lookups.ts
    ├── layout/shell/                    # App shell (@layout/*)
    └── features/                        # One folder per screen (@features/*)
```

> **Backend layering** mirrors the SaaSify enterprise solution: `Api → Services (+ Contracts) → Data → Entities`, with generic `Repository.Pattern` / `Service.Pattern` base projects. Services are registered and injected against their contracts.
> **Frontend** uses TypeScript path aliases (`@core`, `@shared`, `@features`, `@layout`, `@environments`) configured in `tsconfig.json`.

---

## Data model highlights

- **Deal** — lifecycle, products, pricing, meeting notes, change history
- **MeetingNoteSession** — multiple sessions per deal (title, raw notes, extracted summary, timestamp)
- **ActionItem** — task, owner, due date, status, optional `sessionId`
- **DealReminder** — reminder text, date, type, optional `sessionId`
- **AiExtractedSummary** — `standardFields` + `dynamicFields`
- **DealChangeEntry** — audit log entries with category and timestamp

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `MSB3027` on `dotnet build` | API is running — stop it or use `MarketplaceCopilot.Api/build.ps1` |
| API offline banner on login | Run `dotnet run` in `MarketplaceCopilot.Api/` or `.\start-demo.ps1` |
| Extract Insights fails | Confirm API at http://localhost:5280/api/health |
| Empty deals list | Check `MarketplaceCopilot.Api/data/deals.json` exists; restart API |
| Frontend proxy errors | Ensure Angular dev server uses `proxy.conf.json` |

---

## Not yet built (future / post-hackathon)

- Approvals workflow screen (step 5 in stepper)
- Pipeline / kanban view
- Real database (SQL Server / PostgreSQL)
- Production OpenAI / Azure OpenAI integration
- Email sending for verification flow
- Admin role assignment panel
- Reports, Settings, Notifications menu items

---

## Upgrading to real AI (optional)

1. Add API key to `MarketplaceCopilot.Api/appsettings.json`:

   ```json
   "Ai": {
     "OpenAiApiKey": "sk-..."
   }
   ```

2. Wire `AiService` to call OpenAI for extraction and chat (currently rule-based for reliable offline demos).

---

## Key talking points for judges

1. **End-to-end deal workflow** — create → products → pricing → notes → tracking (not just a mockup)
2. **AI that adds structure** — meeting notes become searchable sessions with dynamic fields
3. **Operational tracking** — action items, reminders, and audit log in tabbed UI beside each deal
4. **Enterprise list UX** — search, sort, pagination on Home and Deals (reusable pattern)
5. **Live pricing engine** — complex marketplace pricing rules with preview before save
6. **Demo-ready** — runs locally in minutes, works without cloud AI keys

---

*Last updated: June 2026 — Marketplace Copilot hackathon build*
