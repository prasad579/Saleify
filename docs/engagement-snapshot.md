# Engagement Snapshot & Executive Summary — Design Notes

Feature that lets Sales, Marketplace Managers, and Event Owners generate an
executive engagement summary and email it directly from the app — reducing the
manual effort of preparing status updates.

> Status: **V1 implemented.** All entry points, all five report sections, Copy /
> Download PDF / Email actions, deep links, and the dashboard insights card are live.

---

## Domain mapping

The spec talks about "Engagements" and "Events"; these map onto existing entities —
no new core models were introduced for the data itself.

| Spec concept        | Existing entity / field |
|---------------------|--------------------------|
| Engagement          | `Deal` (`EngagementType`, `Owner`, `Stage`, `MarketplaceStatus`, `ExpectedValue`, `Marketplace`, `ExpectedCloseDate`) |
| Event / Campaign    | `CampaignEvent` (computed `Status`: Upcoming / Active / Completed) |
| Event Tag           | `Deal.CampaignEventId` / `CampaignEventName` |
| Private Offer       | `Deal` where `EngagementType == "Private Offer"` |
| Pipeline value      | sum of `Deal.ExpectedValue` |

---

## Snapshot scopes

One request shape (`SnapshotRequest`) drives every surface via a `scope` discriminator:

| Scope        | Resolves to | Raised from |
|--------------|-------------|-------------|
| `engagement` | a single deal (`dealId`) | Individual engagement (deal overview) |
| `event`      | all deals tagged to a campaign (`eventId`) | Campaign / Event details row |
| `filtered`   | deals matching owner/stage/status/tag/search (+ `openOnly`) | Engagements list (honours active filters) |
| `dashboard`  | all accessible engagements | Home "Engagement Insights" card (leadership summary) |

Home's header buttons use `filtered` scoped to the current user (`openOnly`), giving
a personal "my open engagements" snapshot; the Insights card uses `dashboard` for the
org-wide leadership view.

---

## Output structure (5 sections)

1. **Event Information** — only rendered when an event tag applies to the scope.
2. **Engagement Summary** — total + per-type counts (dynamic; covers Workshops,
   Hackathons, POCs, Free Trials, Private Offers, and any other type present).
3. **Pipeline Summary** — expected pipeline value + active private-offer count.
4. **Engagements Requiring Attention** — open engagements with a derived status
   (Approval Pending / Pending Follow-Up / Trial Running / In Progress) and the next
   action date (earliest pending action item → reminder → expected close date).
5. **Active Private Offers** — open Private-Offer engagements with offer value.

Every row in sections 4 & 5 supports **View** (deep link → `/deals/:id`) and
**Copy Link** (full absolute URL built from `Auth:FrontendUrl`).

### Workshop summary layout

For a single **Workshop** engagement (`scope == "engagement"` and `EngagementType == "Workshop"`,
surfaced via `EngagementSnapshot.EngagementType`), the on-screen modal and the PDF render in the
**same compact, email-style layout** the email already uses — bullet lists for the Event /
Engagement / Pipeline sections (instead of the metric-card grid), plus the configurable
**intro** and **footer** lines from Settings. Other engagement types keep the metric-card layout.

---

## Actions

- **Copy Summary** — plain-text rendering to the clipboard.
- **Download PDF** — opens a self-contained, print-styled HTML page in a new tab and
  triggers the browser print dialog (Save as PDF). No server dependency.
- **Email Summary** — compose panel (To / Cc / Subject prefilled) → `POST /api/snapshot/email`.

### Email delivery (simulated, SMTP-ready)

The email body is a compact, **table + bullet** HTML layout (no long AI paragraphs,
capped at ~8 rows per table to stay under a page). Delivery is gated on configuration:

- **No SMTP configured (default / demo):** the body is generated and returned for
  preview; the response says *"demo mode … not actually delivered."*
- **SMTP configured:** set `Email:Smtp:Host` (+ `Port`, `Username`, `Password`,
  `EnableSsl`) and `Email:From` in `appsettings.json`; the same body is sent for real.

This mirrors how `AiService` is offline-by-default but upgradeable.

---

## Files

### Backend (`MarketplaceCopilot.*`)
- `Entities/Models/SnapshotModels.cs` — request/response + section DTOs.
- `Services.Contracts/ISnapshotService.cs`
- `Services/SnapshotService.cs` — set resolution, section builders, email render/send.
- `Api/Controllers/SnapshotController.cs` — `POST /api/snapshot`, `POST /api/snapshot/email`.
- `Api/Controllers/DashboardController.cs` — `GET /api/dashboard/insights`.
- `Program.cs` — DI registration.

### Frontend (`frontend/src/app`)
- `shared/data/snapshot.model.ts` — shared TS contracts.
- `core/services/snapshot-launcher.service.ts` — signal-driven open/close state.
- `shared/components/engagement-snapshot/` — the global modal (mounted once in the shell).
- `core/services/api.service.ts` — `generateSnapshot`, `emailSnapshot`, `getDashboardInsights`.
- Wired into: `layout/shell` (mount point), `features/home` (buttons + Insights card),
  `features/deals-list` (buttons + CSV export), `features/campaign-events` (per-row),
  `features/deal-overview` (per-engagement).

---

## Permissions

Any authenticated user can generate a snapshot. The snapshot reflects the engagements
already returned by the deal store (visibility is enforced upstream by the existing
deal access rules).

---

## Not in V1 (future)

AI-generated recommendations · weekly scheduled executive summaries · automated email
distribution · trend analysis · event conversion analytics · revenue forecasting.
