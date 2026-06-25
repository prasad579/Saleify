import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { SnapshotLauncherService } from '@core/services/snapshot-launcher.service';
import { SnapshotSettingsService } from '@core/services/snapshot-settings.service';
import { ConfirmDialogService } from '@core/services/confirm-dialog.service';
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
import { stepperSteps } from '@shared/utils/engagement.util';

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
  private router = inject(Router);
  private launcher = inject(SnapshotLauncherService);
  snapSettings = inject(SnapshotSettingsService);
  private confirm = inject(ConfirmDialogService);

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

  snapshot(email = false) {
    if (this.dealId) this.launcher.launch({ scope: 'engagement', dealId: this.dealId }, { email });
  }

  private countsLine(): string {
    const tasks = this.actionItems?.length || 0;
    const reminders = this.reminders?.length || 0;
    return `It has ${tasks} task(s) and ${reminders} reminder(s).`;
  }

  async archive() {
    const ok = await this.confirm.open({
      title: `Archive “${this.deal?.name || this.dealId}”?`,
      lines: [
        'Archiving hides this engagement from your dashboards and lists, together with its tasks and reminders.',
        this.countsLine(),
        'Nothing is deleted — you can restore it anytime from the Archived view.'
      ],
      confirmLabel: 'Archive'
    });
    if (!ok) return;
    this.api.archiveDeal(this.dealId).subscribe(() => this.router.navigate(['/deals'], { queryParams: { archived: 1 } }));
  }

  restore() {
    this.api.unarchiveDeal(this.dealId).subscribe(() => { if (this.deal) this.deal.archived = false; });
  }

  async remove() {
    const ok = await this.confirm.open({
      title: `Permanently delete “${this.deal?.name || this.dealId}”?`,
      lines: [
        'This permanently removes the engagement and everything attached to it — pricing, meeting notes, tasks, reminders, and full change history.',
        this.countsLine(),
        'This cannot be undone. The engagement cannot be retrieved once deleted.'
      ],
      confirmLabel: 'Delete permanently',
      danger: true
    });
    if (!ok) return;
    this.api.deleteDeal(this.dealId).subscribe(() => this.router.navigate(['/deals']));
  }

  /** Workflow links limited to the screens that apply to this engagement type. */
  get workflowLinks(): { label: string; path: any[] }[] {
    return stepperSteps(this.deal?.engagementType || 'Private Offer').map(s => ({
      label: s.label,
      path: s.key === 'details' ? ['/deals', this.dealId, 'edit'] : ['/deals', this.dealId, s.key]
    }));
  }
}
