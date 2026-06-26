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
import { ScreenKey, stepperSteps } from '@shared/utils/engagement.util';

interface ProgressStep {
  key: ScreenKey;
  label: string;
  hint: string;
  done: boolean;
  current: boolean;
  path: any[];
}

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

  // Workflow completion (computed once after load).
  steps: ProgressStep[] = [];
  completedCount = 0;
  totalSteps = 0;
  percentComplete = 0;
  allComplete = false;

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
        this.computeProgress();
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  private static readonly STEP_HINTS: Record<ScreenKey, string> = {
    'details': 'Customer, contact, marketplace & tag',
    'products': 'Choose the products in the offer',
    'pricing': 'Set discount, duration & payment terms',
    'meeting-notes': 'Capture notes, action items & reminders',
    'approvals': 'Run reviews & generate documents'
  };

  /** Build the per-section progress list, marking what's complete and which step is current. */
  private computeProgress() {
    const d = this.deal;
    const steps = stepperSteps(d?.engagementType || 'Private Offer').map(s => ({
      key: s.key,
      label: s.label,
      hint: DealOverviewComponent.STEP_HINTS[s.key] ?? '',
      done: this.isStepComplete(s.key),
      current: false,
      path: s.key === 'details' ? ['/deals', this.dealId, 'edit'] : ['/deals', this.dealId, s.key]
    } as ProgressStep));

    // The current step is the first one that isn't done yet.
    const next = steps.find(s => !s.done);
    if (next) next.current = true;

    this.steps = steps;
    this.totalSteps = steps.length;
    this.completedCount = steps.filter(s => s.done).length;
    this.percentComplete = this.totalSteps ? Math.round((this.completedCount * 100) / this.totalSteps) : 0;
    this.allComplete = this.totalSteps > 0 && this.completedCount === this.totalSteps;
  }

  private isStepComplete(key: ScreenKey): boolean {
    const d = this.deal;
    if (!d) return false;
    switch (key) {
      case 'details':
        return !!(d.engagementType && d.contactEmail && d.owner && !d.quickCapture);
      case 'products':
        return (d.productIds?.length || 0) > 0;
      case 'pricing':
        return !!(d.pricing && d.pricing.netContractValue > 0);
      case 'meeting-notes':
        return (d.meetingNotes?.sessions?.length || 0) > 0 || !!d.meetingNotes?.rawNotes;
      case 'approvals':
        return ['In Review', 'Published', 'Completed'].includes(d.marketplaceStatus) || !!d.approvals?.documentsLocked;
      default:
        return false;
    }
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
}
