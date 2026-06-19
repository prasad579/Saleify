import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ApiService } from '@core/services/api.service';
import { LastMeetingSnapshotComponent } from '@shared/components/last-meeting-snapshot/last-meeting-snapshot.component';
import { DealTrackingPanelComponent } from '@shared/components/deal-tracking-panel/deal-tracking-panel.component';
import { DealStepperComponent } from '@shared/components/deal-stepper/deal-stepper.component';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';
import {
  ActionItemRow,
  ChangeHistoryRow,
  ExtractedSummary,
  MeetingSessionRow,
  ReminderRow,
  SummaryField,
  mergeActionItems,
  newId,
  normalizeActionItems,
  normalizeExtracted,
  normalizeHistory,
  normalizeReminders,
  normalizeSessions,
  getAllSummaryFields,
  meaningfulFields
} from '@shared/utils/meeting-notes.util';

@Component({
  selector: 'app-deal-meeting-notes',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, LastMeetingSnapshotComponent, DealTrackingPanelComponent, DealStepperComponent, DealFlowFooterComponent],
  templateUrl: './deal-meeting-notes.component.html',
  styleUrl: './deal-meeting-notes.component.scss'
})
export class DealMeetingNotesComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  dealId = '';
  deal: any = null;
  notes = '';
  sessionTitle = '';
  sessions: MeetingSessionRow[] = [];
  draftExtracted: ExtractedSummary | null = null;
  pendingSessionId = '';
  actionItems: ActionItemRow[] = [];
  reminders: ReminderRow[] = [];
  changeHistory: ChangeHistoryRow[] = [];
  insight = '';
  loading = true;
  saving = false;
  extracting = false;
  error = '';
  linkNewItemsToSession = '';

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    this.loadDeal();
  }

  get lastSession(): MeetingSessionRow | null {
    return this.sessions[0] ?? null;
  }

  get draftFields(): SummaryField[] {
    return meaningfulFields(getAllSummaryFields(this.draftExtracted));
  }

  loadDeal() {
    this.loading = true;
    this.api.getDeal(this.dealId).subscribe({
      next: (detail: any) => {
        this.deal = detail.deal;
        const saved = detail.deal?.meetingNotes;
        this.sessions = normalizeSessions(saved?.sessions, saved);
        this.actionItems = normalizeActionItems(saved?.actionItems || []);
        this.reminders = normalizeReminders(saved?.reminders || []);
        this.changeHistory = normalizeHistory(detail.deal?.changeHistory || []);
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  extract() {
    if (!this.notes.trim()) {
      this.error = 'Type or paste meeting notes first.';
      return;
    }
    if (!this.pendingSessionId) this.pendingSessionId = newId();
    this.error = '';
    this.extracting = true;
    this.api.extractInsights(this.notes, this.dealId).subscribe({
      next: (res: any) => {
        this.draftExtracted = normalizeExtracted(res.summary);
        const suggested = normalizeActionItems(res.actionItems || []);
        this.actionItems = mergeActionItems(this.actionItems, suggested, this.pendingSessionId);
        this.linkNewItemsToSession = this.pendingSessionId;
        this.insight = res.insight;
        this.extracting = false;
      },
      error: () => {
        this.error = 'Could not extract insights. Is the API running?';
        this.extracting = false;
      }
    });
  }

  save() {
    this.saving = true;
    this.error = '';

    let sessionIdForSave = this.pendingSessionId;
    const payload: any = {
      sessions: this.sessions.map(s => ({
        id: s.id,
        title: s.title,
        rawNotes: s.rawNotes,
        extracted: s.extracted,
        createdAt: s.createdAt
      })),
      actionItems: this.actionItems.map(a => ({ ...a })),
      reminders: this.reminders.map(r => ({ ...r }))
    };

    if (this.notes.trim()) {
      sessionIdForSave = sessionIdForSave || newId();
      payload.newSession = {
        id: sessionIdForSave,
        title: this.sessionTitle.trim() || `Meeting — ${new Date().toLocaleDateString()}`,
        rawNotes: this.notes.trim(),
        extracted: this.draftExtracted,
        createdAt: new Date().toISOString()
      };
      payload.actionItems = payload.actionItems.map((a: ActionItemRow) =>
        a.sessionId === this.pendingSessionId || (!a.sessionId && a.source === 'ai')
          ? { ...a, sessionId: sessionIdForSave }
          : a
      );
      payload.reminders = payload.reminders.map((r: ReminderRow) =>
        r.sessionId === this.pendingSessionId ? { ...r, sessionId: sessionIdForSave } : r
      );
    }

    this.api.setMeetingNotes(this.dealId, payload).subscribe({
      next: (res: any) => {
        this.applySaveResult(res);
      },
      error: () => {
        this.saving = false;
        this.error = 'Could not save meeting notes.';
      }
    });
  }

  private applySaveResult(res: any) {
    this.saving = false;
    this.insight = res.insight || 'Meeting notes saved.';
    if (res.deal?.deal) {
      this.deal = res.deal.deal;
      const saved = res.deal.deal.meetingNotes;
      this.sessions = normalizeSessions(saved?.sessions, saved);
      this.actionItems = normalizeActionItems(saved?.actionItems || []);
      this.reminders = normalizeReminders(saved?.reminders || []);
      this.changeHistory = normalizeHistory(res.deal.deal.changeHistory || []);
    }
    this.notes = '';
    this.sessionTitle = '';
    this.draftExtracted = null;
    this.pendingSessionId = '';
    this.linkNewItemsToSession = '';
  }

  proceedToApprovals() {
    this.saving = true;
    this.error = '';

    const payload: any = {
      sessions: this.sessions.map(s => ({
        id: s.id,
        title: s.title,
        rawNotes: s.rawNotes,
        extracted: s.extracted,
        createdAt: s.createdAt
      })),
      actionItems: this.actionItems.map(a => ({ ...a })),
      reminders: this.reminders.map(r => ({ ...r }))
    };

    if (this.notes.trim()) {
      const sessionIdForSave = this.pendingSessionId || newId();
      payload.newSession = {
        id: sessionIdForSave,
        title: this.sessionTitle.trim() || `Meeting — ${new Date().toLocaleDateString()}`,
        rawNotes: this.notes.trim(),
        extracted: this.draftExtracted,
        createdAt: new Date().toISOString()
      };
    }

    this.api.setMeetingNotes(this.dealId, payload).subscribe({
      next: (res: any) => {
        this.applySaveResult(res);
        this.api.enterApprovals(this.dealId).subscribe({
          next: () => this.router.navigate(['/deals', this.dealId, 'approvals']),
          error: () => {
            this.saving = false;
            this.router.navigate(['/deals', this.dealId, 'approvals']);
          }
        });
      },
      error: () => {
        this.saving = false;
        this.error = 'Could not save meeting notes.';
      }
    });
  }
}
