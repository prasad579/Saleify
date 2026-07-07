import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import { SnapshotLauncherService } from '@core/services/snapshot-launcher.service';
import { SnapshotSettingsService } from '@core/services/snapshot-settings.service';
import { HomeSettingsService } from '@core/services/home-settings.service';
import { AttentionSettingsService } from '@core/services/attention-settings.service';
import { DashboardInsights } from '@shared/data/snapshot.model';
import { CampaignEvent, eventStatus } from '@shared/data/lookups';
import { formatReminderDate, reminderBadgeClass, reminderStatus } from '@shared/utils/meeting-notes.util';
import {
  SortOrder,
  formatCreatedDate,
  paginateSlice,
  sortByCreatedAt,
  totalPages
} from '@shared/utils/pagination.util';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  private launcher = inject(SnapshotLauncherService);
  snapSettings = inject(SnapshotSettingsService);
  homeSettings = inject(HomeSettingsService);
  attentionSettings = inject(AttentionSettingsService);
  auth = inject(AuthService);
  data: any = null;
  insights: DashboardInsights | null = null;
  events: CampaignEvent[] = [];
  deals: any[] = [];
  chatMessage = '';
  chatReply = '';
  continuePath = dealContinuePath;

  readonly pageSize = 5;
  dealsSort: SortOrder = 'newest';
  dealsPage = 1;
  tasksPage = 1;
  remindersPage = 1;

  // My Tasks card: list vs grouped-by-engagement, and show-all toggle.
  tasksView: 'list' | 'grouped' = 'list';
  showAllTasks = false;

  formatCreatedDate = formatCreatedDate;
  formatReminderDate = formatReminderDate;
  reminderStatus = reminderStatus;
  reminderBadgeClass = reminderBadgeClass;

  // ---- "Needs attention today" alert (overdue + due-today items) ----
  private readonly todayStr = new Date().toISOString().slice(0, 10);
  private static readonly DISMISS_KEY = 'mc_home_alert_dismissed';
  alertDismissed = this.readDismissed();

  private readDismissed(): boolean {
    try { return localStorage.getItem(HomeComponent.DISMISS_KEY) === this.todayStr; } catch { return false; }
  }

  dismissAlert() {
    this.alertDismissed = true;
    try { localStorage.setItem(HomeComponent.DISMISS_KEY, this.todayStr); } catch { /* ignore */ }
  }

  /**
   * Every dated item (tasks, reminders, engagement close dates) mapped to a uniform row, honoring
   * the source toggles from Settings → Alerts & Reminders. Days < 0 = overdue, 0 = today.
   */
  private get datedItems(): { icon: string; kind: string; text: string; context: string; link: any[]; tone: string; label: string; days: number }[] {
    const s = this.attentionSettings.settings();
    const items: any[] = [];
    if (s.includeTasks) {
      for (const t of (this.data?.tasks || [])) {
        if (t.status === 'Done') continue;
        const st = reminderStatus(t.dueDate);
        if (st.days === null) continue;
        items.push({ icon: '✅', kind: 'Task', text: t.task, context: t.customer || t.dealName || t.deal,
          link: ['/deals', t.deal, 'meeting-notes'], tone: st.tone, label: st.label, days: st.days });
      }
    }
    if (s.includeReminders) {
      for (const r of (this.data?.reminders || [])) {
        const st = reminderStatus(r.dateTime);
        if (st.days === null) continue;
        items.push({ icon: '⏰', kind: 'Reminder', text: r.reminder, context: r.customer || r.dealName || r.deal,
          link: ['/deals', r.deal, 'meeting-notes'], tone: st.tone, label: st.label, days: st.days });
      }
    }
    if (s.includeEngagements) {
      for (const d of (this.deals || [])) {
        if (d.archived) continue;
        const st = reminderStatus(d.expectedCloseDate);
        if (st.days === null) continue;
        items.push({ icon: '💼', kind: 'Engagement', text: d.name || d.customer, context: 'Target close ' + formatReminderDate(d.expectedCloseDate),
          link: ['/deals', d.id], tone: st.tone, label: st.label, days: st.days });
      }
    }
    return items;
  }

  /** Urgent: overdue + due today (the top alert). Overdue first. */
  get attentionList() {
    return this.datedItems.filter(i => i.days <= 0).sort((a, b) => a.days - b.days);
  }

  /** Upcoming: due within the configured look-ahead window (not today/overdue). Soonest first. */
  get upcomingList() {
    const window = this.attentionSettings.settings().upcomingWindowDays || 7;
    return this.datedItems.filter(i => i.days > 0 && i.days <= window).sort((a, b) => a.days - b.days);
  }

  get attentionTotal(): number { return this.attentionList.length; }
  get missedCount(): number { return this.attentionList.filter(i => i.tone === 'overdue').length; }
  get dueTodayCount(): number { return this.attentionList.filter(i => i.tone === 'today').length; }
  get showAlert(): boolean { return this.attentionSettings.settings().alertEnabled && !this.alertDismissed && this.attentionTotal > 0; }
  get showUpcoming(): boolean { return this.attentionSettings.settings().upcomingEnabled && this.upcomingList.length > 0; }
  get upcomingWindowDays(): number { return this.attentionSettings.settings().upcomingWindowDays || 7; }

  // ---- Stat card + tag redirections ----
  private get myName(): string { return this.auth.user()?.name || 'Srinivas K'; }

  goMyEngagements() { this.router.navigate(['/deals'], { queryParams: { owner: this.myName, scope: 'open' } }); }
  goApprovals() { this.router.navigate(['/deals'], { queryParams: { stage: 'Approval' } }); }
  goOffers() { this.router.navigate(['/deals'], { queryParams: { status: 'In Review' } }); }
  goTag(ev: CampaignEvent) { this.router.navigate(['/deals'], { queryParams: { tag: ev.name } }); }
  goEngagement(id: string) { if (id) this.router.navigate(['/deals', id]); }

  // ---- Engagement Snapshot ----
  /** Personal snapshot — my open engagements. */
  snapshotMine(email = false) {
    this.launcher.launch({ scope: 'filtered', owner: this.myName, openOnly: true }, { email });
  }

  /** Org-wide leadership summary across all engagements. */
  snapshotLeadership(email = false) {
    this.launcher.launch({ scope: 'dashboard' }, { email });
  }

  // ---- Grouped tasks ----
  get groupedTasks(): { dealId: string; dealName: string; customer: string; items: any[] }[] {
    const groups = new Map<string, { dealId: string; dealName: string; customer: string; items: any[] }>();
    for (const t of (this.data?.tasks || [])) {
      const key = t.deal || 'unassigned';
      if (!groups.has(key)) {
        groups.set(key, { dealId: t.deal, dealName: t.dealName || t.deal, customer: t.customer || '', items: [] });
      }
      groups.get(key)!.items.push(t);
    }
    return [...groups.values()];
  }

  toggleTasksView() { this.tasksView = this.tasksView === 'list' ? 'grouped' : 'list'; this.tasksPage = 1; }

  ngOnInit() {
    this.refresh();
  }

  refresh() {
    this.api.getDashboard().subscribe(d => {
      this.data = d;
      this.dealsPage = 1;
      this.tasksPage = 1;
      this.remindersPage = 1;
    });
    this.api.getCampaignEvents().subscribe((e: any) => {
      this.events = Array.isArray(e) ? e : [];
    });
    this.api.getDeals().subscribe((d: any) => {
      this.deals = Array.isArray(d) ? d : [];
    });
    this.api.getDashboardInsights().subscribe({
      next: i => this.insights = i,
      error: () => this.insights = null
    });
  }

  statusOf(ev: CampaignEvent): string {
    return ev.status || eventStatus(ev.startDate, ev.endDate);
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Active': return 'badge-green';
      case 'Upcoming': return 'badge-blue';
      case 'Completed': return 'badge-orange';
      default: return 'badge-gray';
    }
  }

  dealCountFor(ev: CampaignEvent): number {
    return this.deals.filter(d => d.campaignEventId === ev.id).length;
  }

  get sortedDeals(): any[] {
    return sortByCreatedAt(this.data?.openDealsList || [], this.dealsSort);
  }

  get pagedDeals(): any[] {
    return paginateSlice(this.sortedDeals, this.dealsPage, this.pageSize);
  }

  get dealsTotalPages(): number {
    return totalPages(this.sortedDeals.length, this.pageSize);
  }

  get pagedTasks(): any[] {
    const all = this.data?.tasks || [];
    if (this.showAllTasks) return all;
    return paginateSlice(all, this.tasksPage, this.pageSize);
  }

  get tasksTotalPages(): number {
    return totalPages((this.data?.tasks || []).length, this.pageSize);
  }

  get tasksCount(): number {
    return (this.data?.tasks || []).length;
  }

  /** Reminders sorted by urgency: overdue/soonest first, undated last. */
  get sortedReminders(): any[] {
    const rows = [...(this.data?.reminders || [])];
    return rows.sort((a, b) => {
      const da = this.reminderStatus(a.dateTime).days;
      const db = this.reminderStatus(b.dateTime).days;
      if (da === null && db === null) return 0;
      if (da === null) return 1;
      if (db === null) return -1;
      return da - db;
    });
  }

  get pagedReminders(): any[] {
    return paginateSlice(this.sortedReminders, this.remindersPage, this.pageSize);
  }

  get remindersTotalPages(): number {
    return totalPages((this.data?.reminders || []).length, this.pageSize);
  }

  onDealsSortChange() {
    this.dealsPage = 1;
  }

  prevDealsPage() {
    if (this.dealsPage > 1) this.dealsPage--;
  }

  nextDealsPage() {
    if (this.dealsPage < this.dealsTotalPages) this.dealsPage++;
  }

  prevTasksPage() {
    if (this.tasksPage > 1) this.tasksPage--;
  }

  nextTasksPage() {
    if (this.tasksPage < this.tasksTotalPages) this.tasksPage++;
  }

  prevRemindersPage() {
    if (this.remindersPage > 1) this.remindersPage--;
  }

  nextRemindersPage() {
    if (this.remindersPage < this.remindersTotalPages) this.remindersPage++;
  }

  askCopilot() {
    if (!this.chatMessage.trim()) return;
    this.api.copilotChat(this.chatMessage).subscribe(r => this.chatReply = r.reply);
  }

  // ---- Recent activity ----
  /** Emoji marker per change-history category. */
  activityIcon(category: string): string {
    switch (category) {
      case 'Deal': return '📝';
      case 'Products': return '📦';
      case 'Pricing': return '💲';
      case 'Meeting Notes': return '🗒️';
      case 'Approvals': return '✅';
      case 'Engagement': return '🚀';
      case 'Document': return '📄';
      case 'System': return '⚙️';
      default: return '•';
    }
  }

  activityBadgeClass(category: string): string {
    switch (category) {
      case 'Pricing': return 'badge-blue';
      case 'Approvals': return 'badge-green';
      case 'Engagement': return 'badge-green';
      case 'Meeting Notes': return 'badge-purple';
      case 'Products': return 'badge-orange';
      default: return 'badge-gray';
    }
  }

  /** "2026-06-24 17:35:00 UTC" → a friendly relative time, falling back to a date. */
  formatActivityTime(ts: string): string {
    if (!ts) return '';
    const d = new Date(ts.replace(' UTC', 'Z').replace(' ', 'T'));
    if (isNaN(d.getTime())) return ts;
    const diffMs = Date.now() - d.getTime();
    if (diffMs < 0) return d.toLocaleDateString();
    const min = Math.floor(diffMs / 60000);
    if (min < 1) return 'just now';
    if (min < 60) return `${min}m ago`;
    const hr = Math.floor(min / 60);
    if (hr < 24) return `${hr}h ago`;
    const day = Math.floor(hr / 24);
    if (day < 7) return `${day}d ago`;
    return d.toLocaleDateString();
  }
}
