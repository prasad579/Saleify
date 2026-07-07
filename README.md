# Saleify

**AI-powered deal desk for cloud marketplace sellers.** Guide an engagement from creation through product selection, pricing, meeting-note capture, and multi-step approvals — then push it as an **offer request** to SaaSify / a marketplace and capture the response — with rule-based AI assistance and an application-wide audit trail throughout. Almost everything is **customizable from Settings**. Built as a layered .NET 9 solution with an Angular frontend.

> 🎯 Full demo walkthrough (feature tour, script for judges, troubleshooting): **[HACKATHON-DEMO.md](./HACKATHON-DEMO.md)**
> 🔐 Google / Microsoft sign-in setup: **[AUTH_SETUP.md](./AUTH_SETUP.md)**

![Saleify dashboard](docs/screenshots/02-dashboard.png)

---

## Quick start

```powershell
.\start-demo.ps1
```

Open **http://localhost:4200** → click **Try Demo Login** (`demo@marketplace.com` / `demo123`).

The script launches the API (`:5280`) and the Angular dev server (`:4200`) in separate windows.

---

## Features

| Area | What it does |
|------|--------------|
| **Auth** | Demo login, email/password signup flow, Google & Microsoft OAuth (stubs until credentials added) |
| **Home dashboard** | KPIs, insights, open-deal list, tasks, reminders, recent activity, campaign tags — every card can be **shown/hidden from Settings → Home Dashboard** |
| **Engagements list** | Search, created-date sorting, pagination, and an **interactive filter builder** (Marketplace / Tag / Stage / Status / Owner / Type) combined with AND, plus CSV export |
| **Engagement workflow** | Create → select products → configure pricing → capture meeting notes → approvals. The create screen has a **sticky AI-playbook sidebar (show/hide)**, a vertical **workflow stepper**, and **inline field-level validation** |
| **Engagement types** | The catalog of types and which sections (products / pricing / meeting notes / approvals) apply to each is **fully configurable** — e.g. turn approvals off for a Free Trial — from Settings → Engagement Types |
| **Engagement progress** | Opening an engagement shows a **progress card** — percent bar, per-section checklist with ✓ tick marks, and a pulsing "current step" — so it's obvious what's done and what's next |
| **Pricing calculator** | Duration models, per-year discounts, flexible payment schedules, pro-rate, marketplace fees, live preview |
| **Meeting notes + AI** | Extracts standard & dynamic fields and suggests dated action items from raw notes |
| **Approvals** | Rule-driven approval steps (finance / legal / marketplace), document generation (pricing, legal, EULA), audit log, re-approval on change |
| **Offer Requests** | Every submitted engagement is recorded as an offer request — the **exact JSON payload pushed** to the destination, rich filtering (type / marketplace / product / stage / status / response + date range), a full **detail screen** (request/response JSON, copy & download), and **capture-response** to record acceptance/rejection back |
| **Submission lock** | Once pushed, an engagement is **locked from edits** so it can't silently diverge from what was sent. **Unlock-to-revise** re-opens it; edits flag the offer request as out of date until re-submitted |
| **Global Audit Log** | Every change across the app — engagements, pricing, approvals, offer requests, and **all settings** — recorded with **who / when / what / details**, filterable by category and free-text, attributed to the signed-in user |
| **Change history** | Every deal mutation logged per-engagement with category, summary, and timestamp |
| **Toasts & validation** | Save actions surface **toast notifications**; forms show validation **inline next to each field** |

All AI is **rule-based and offline** — no external API key required for the demo.

---

## Screenshots

| Sign in | Engagements |
|---------|-------------|
| ![Login](docs/screenshots/01-login.png) | ![Deals list](docs/screenshots/03-deals.png) |

| Pricing calculator | Approvals |
|--------------------|-----------|
| ![Pricing](docs/screenshots/05-pricing.png) | ![Approvals](docs/screenshots/06-approvals.png) |

| Deal overview | Meeting notes + AI |
|---------------|--------------------|
| ![Deal overview](docs/screenshots/04-deal-overview.png) | ![Meeting notes](docs/screenshots/07-meeting-notes.png) |

---

## Architecture

The backend mirrors a classic enterprise layering (modeled on the SaaSify reference solution): each layer is its own project, and services are registered and injected **against their contracts**.

```
                         ┌─────────────────────────────┐
  HTTP / Angular  ─────► │   MarketplaceCopilot.Api     │  Controllers, Program.cs, DI
                         └──────────────┬──────────────┘
                                        │ depends on
                ┌───────────────────────┼───────────────────────┐
                ▼                       ▼                        ▼
   MarketplaceCopilot.Services   .Services.Contracts     MarketplaceCopilot.Data
   (Deal, Pricing, Approval,     (IDealService,            (DataStore, UserStore —
    OfferRequest, Audit, Ai…)     IOfferRequestService,     JSON / in-memory)
                │                  IAuditService, …)              │
                └───────────────────┬───────────────────────────┘
                                    ▼
                       MarketplaceCopilot.Entities
                       (Models/ + Dtos/ — domain types)

   Generic bases:  Repository.Pattern  (IObjectState, IRepository<T>, UnitOfWork)
                   Service.Pattern     (IService<T>, Service<T>)
```

**Dependency flow:** `Api → Services (+ Contracts) → Data → Entities`, with the generic `Repository.Pattern` / `Service.Pattern` projects underneath.

**Audit attribution:** the SPA sends the signed-in user in an `X-Acting-User` header; an `IActingUserAccessor` (implemented over `IHttpContextAccessor` in the API layer) lets the service layer record *who* made each change without depending on ASP.NET.

The Angular app uses a **core / shared / features** layout with TypeScript path aliases:

| Alias | Folder | Holds |
|-------|--------|-------|
| `@core/*` | `src/app/core` | App-wide singletons — `services/`, `guards/`, `interceptors/` |
| `@shared/*` | `src/app/shared` | Reusable `components/`, `utils/`, `data/` (models) |
| `@features/*` | `src/app/features` | One folder per screen |
| `@layout/*` | `src/app/layout` | App shell |
| `@environments/*` | `src/environments` | Environment config |

Engagement-type settings load once at app startup (an `APP_INITIALIZER`) and drive the create picker, the stepper, and which screens apply — so toggling a type or its sections in Settings takes effect app-wide.

---

## Tech stack

- **Backend:** .NET 9 Web API (controllers), layered class-library projects, cookie + OAuth authentication, JSON file persistence
- **Frontend:** Angular (standalone components), TypeScript, SCSS, signal-based services, HTTP interceptor
- **AI:** rule-based extraction/insights (offline); optional OpenAI key supported via config
- **Tooling:** `dotnet` CLI, `npm` / Angular CLI

---

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org) and npm

### Build everything

```powershell
dotnet build MarketplaceCopilot.sln       # backend solution
cd frontend && npm install                 # frontend deps (first run)
```

### Run manually

```powershell
# Terminal 1 — backend (or: dotnet run --project MarketplaceCopilot.Api)
cd MarketplaceCopilot.Api && dotnet run

# Terminal 2 — frontend
cd frontend && npm start
```

API → http://localhost:5280 · UI → http://localhost:4200

---

## Project structure

```
Marketplace-Copilot/
├── MarketplaceCopilot.sln
├── MarketplaceCopilot.Api/                  # Presentation: REST controllers + Program.cs
│   ├── Controllers/                         #   Deals, Auth, Lookups, Dashboard, Products,
│   │                                        #   ApprovalRules, EngagementTypes, HomeSettings,
│   │                                        #   OfferRequests, Audit, People, Playbooks, …
│   ├── HttpActingUserAccessor.cs            #   resolves the acting user from the request
│   └── data/*.json                          #   demo data + settings (read from content root)
├── MarketplaceCopilot.Services/             # Business logic (DealService, PricingService,
│   │                                        #   ApprovalService, OfferRequestService,
│   │                                        #   AuditService, AiService, …)
├── MarketplaceCopilot.Services.Contracts/   # Service interfaces (+ IActingUserAccessor)
├── MarketplaceCopilot.Data/                 # DataStore (JSON persistence), UserStore
├── MarketplaceCopilot.Entities/             # Models/ + Dtos/
├── Repository.Pattern/                      # Generic repository base
├── Service.Pattern/                         # Generic service base
├── frontend/                                # Angular app
│   └── src/app/
│       ├── core/services/                   #   api, auth, toast, engagement-config,
│       │                                    #   home-settings, snapshot-settings, …
│       ├── core/interceptors/               #   acting-user (X-Acting-User header)
│       ├── features/                        #   home, deals-list, deal-create, deal-overview,
│       │                                    #   offer-requests, offer-request-detail, audit-log,
│       │                                    #   settings (+ engagement-types, home-settings,
│       │                                    #   approvals, people, playbooks, snapshot, events)
│       └── shared/                          #   components (toast, stepper…), utils, data models
├── start-demo.ps1                           # One-click launcher
├── HACKATHON-DEMO.md                        # Full demo guide
└── AUTH_SETUP.md                            # OAuth setup
```

### Data files (`MarketplaceCopilot.Api/data/`)

JSON-backed, created on first run and updated at runtime — no database required:

`deals.json` · `campaign-events.json` · `engagement-playbooks.json` · `snapshot-settings.json` · `approval-rules.json` · `people.json` · `engagement-types.json` · `home-settings.json` · `offer-requests.json` · `audit-log.json`

---

## API reference (selected)

Base URL: `http://localhost:5280`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `GET`  | `/api/health` | Liveness check |
| `GET`  | `/api/dashboard` · `/api/dashboard/insights` | KPIs, tasks, reminders, insights |
| `GET`  | `/api/deals` · `/api/deals/{id}` | List / fetch engagement (with detail) |
| `GET`  | `/api/deals/stats` · `/api/deals/{id}/history` | Pipeline counts · change history |
| `POST` | `/api/deals` · `PUT /api/deals/{id}` | Create / update engagement (blocked when locked) |
| `POST` | `/api/deals/{id}/unlock-edits` | Unlock a submitted engagement to revise |
| `POST` | `/api/deals/{id}/products` · `/pricing/preview` · `/pricing` | Products & pricing (blocked when locked) |
| `POST` | `/api/deals/{id}/meeting-notes` | Save notes + AI insight |
| `GET`  | `/api/deals/{id}/approvals` · `POST .../approvals/action` | Approval summary · approve / reject / changes |
| `POST` | `/api/deals/{id}/approvals/submit` · `/submit-engagement` | Submit (pushes an offer request) |
| `GET`  | `/api/offer-requests` · `/api/offer-requests/{id}` | List / fetch offer requests (with pushed JSON) |
| `POST` | `/api/offer-requests/{id}/response` | Capture the destination's response |
| `GET`  | `/api/audit?category=&entityId=&search=&page=&pageSize=` | Global audit log (filtered, paged) |
| `GET`/`PUT`/`POST` | `/api/engagement-types` (+ `/reset`) | Engagement-type catalog & section config |
| `GET`/`PUT`/`POST` | `/api/home-settings` (+ `/reset`) | Home dashboard card visibility |
| `GET`/`PUT`/`POST` | `/api/approval-rules` (+ `/reset`) | Approval rules |
| `GET`/`PUT`/`POST` | `/api/snapshot/settings` (+ `/reset`) | Snapshot & email settings |
| `GET`/`PUT`/`POST`/`DELETE` | `/api/people` · `/api/campaign-events` · `/api/engagement-playbooks` | Settings catalogs |
| `POST` | `/api/ai/extract-insights` · `/api/ai/chat` | AI extraction / copilot chat |
| `GET`  | `/api/products` · `/api/lookups` | Catalog & dropdown lookups (only **enabled** engagement types) |

> Mutating requests carry an `X-Acting-User` header (set by an Angular interceptor) so audit entries are attributed to the signed-in user.

---

## Settings (everything customizable)

Reachable from the **Settings** screen:

| Setting | What you control |
|---------|------------------|
| **Home Dashboard** | Which cards appear on the home page (stats, insights, tags, open engagements, recent activity, tasks, reminders) |
| **Engagement Types** | Enable/disable each type and choose which sections apply (products / pricing / meeting notes / approvals = required / optional / not applicable), plus tag & marketplace requirements and the submit action |
| **Campaign / Event Tags** | Add, edit, pause, or remove event tags; track conversion |
| **People** | Manage engagement owners — role, enable/disable, restrict by engagement type |
| **Engagement Playbooks** | The "what's next" guidance per type — next steps, talking points, timeline |
| **Snapshot & Email** | Toggle the snapshot/email buttons and which sections/fields appear |
| **Approval Rules** | Which reviews an engagement requires — thresholds, reviewers, applicable types |
| **Audit Log** | View the application-wide audit trail (also in the left nav) |

Every settings change is written to the **Global Audit Log**.

---

## Configuration

- **Demo login:** `demo@marketplace.com` / `demo123` (seeded on startup)
- **OAuth (Google / Microsoft):** add credentials to `MarketplaceCopilot.Api/appsettings.json` — see [AUTH_SETUP.md](./AUTH_SETUP.md)
- **AI provider:** defaults to the built-in rule engine; set `Ai:OpenAiApiKey` in `appsettings.json` to use OpenAI
- **Data & settings:** seed/demo data and all configurable settings persist as JSON in `MarketplaceCopilot.Api/data/` — no database setup required. Delete a file to reseed that area to defaults.

---

## Notes

- This is a hackathon-grade demo: persistence is a set of JSON files, AI is rule-based, and auto-approval is enabled in Development. Harden these (real DB, secrets management, restricted approval, proper identity for audit attribution) before any production use.
- Build the whole backend with `dotnet build` at the repo root; the `MarketplaceCopilot.Api` project is the runnable host.
