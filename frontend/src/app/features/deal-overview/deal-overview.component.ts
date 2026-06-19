import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { LastMeetingSnapshotComponent } from '@shared/components/last-meeting-snapshot/last-meeting-snapshot.component';
import { DealTrackingPanelComponent } from '@shared/components/deal-tracking-panel/deal-tracking-panel.component';
import {
  ActionItemRow,
  ChangeHistoryRow,
  MeetingSessionRow,
  ReminderRow,
  getLastSession,
  normalizeActionItems,
  normalizeHistory,
  normalizeReminders,
  normalizeSessions
} from '@shared/utils/meeting-notes.util';
import { normalizeDealDetail } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-deal-overview',
  standalone: true,
  imports: [RouterLink, DecimalPipe, LastMeetingSnapshotComponent, DealTrackingPanelComponent],
  templateUrl: './deal-overview.component.html',
  styleUrl: './deal-overview.component.scss'
})
export class DealOverviewComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);

  dealId = '';
  deal: any = null;
  loading = true;
  continuePath = dealContinuePath;
  sessions: MeetingSessionRow[] = [];
  actionItems: ActionItemRow[] = [];
  reminders: ReminderRow[] = [];
  changeHistory: ChangeHistoryRow[] = [];

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    this.api.getDeal(this.dealId).subscribe({
      next: (raw: any) => {
        const detail = normalizeDealDetail(raw);
        this.deal = detail.deal;
        const saved = this.deal?.meetingNotes;
        this.sessions = normalizeSessions(saved?.sessions, saved);
        this.actionItems = normalizeActionItems(saved?.actionItems || []);
        this.reminders = normalizeReminders(saved?.reminders || []);
        this.changeHistory = normalizeHistory(this.deal?.changeHistory || []);
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  lastSession() {
    return getLastSession(this.deal);
  }
}
