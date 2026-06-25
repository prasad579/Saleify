import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { SnapshotLauncherService } from '@core/services/snapshot-launcher.service';
import { SnapshotSettingsService } from '@core/services/snapshot-settings.service';
import { ConfirmDialogService } from '@core/services/confirm-dialog.service';
import { SnapshotRequest } from '@shared/data/snapshot.model';
import { formatCreatedDate, paginateSlice, totalPages } from '@shared/utils/pagination.util';
import { EngagementFlag, FlagTone, engagementFlags, rowTone } from '@shared/utils/engagement-flags.util';
import {
  ActionItemRow,
  ReminderRow,
  formatReminderDate,
  normalizeActionItems,
  normalizeReminders,
  reminderBadgeClass,
  reminderStatus
} from '@shared/utils/meeting-notes.util';

@Component({
  selector: 'app-deals-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe, FormsModule],
  templateUrl: './deals-list.component.html',
  styleUrl: './deals-list.component.scss'
})
export class DealsListComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private launcher = inject(SnapshotLauncherService);
  snapSettings = inject(SnapshotSettingsService);
  private confirm = inject(ConfirmDialogService);
  allDeals: any[] = [];
  stats: any = null;
  continuePath = dealContinuePath;

  /** Which set of engagements to show: active (default) or archived. */
  view: 'active' | 'archived' = 'active';
  toast = '';

  search = '';
  // Structured filters set via query params (used by Home card redirections).
  fOwner = '';
  fStage = '';
  fStatus = '';
  fTag = '';
  fScope = ''; // 'open' = exclude closed/abandoned

  private readonly openStatuses = ['Draft', 'In Review', 'Waiting for Info', 'Lead', 'Quick Capture'];

  // Column sorting (click a header to sort; click again to flip direction).
  sortKey = 'createdAt';
  sortDir: 'asc' | 'desc' = 'desc';

  // Row action (kebab) menu + the activity (tasks/reminders) quick-view popup.
  openMenuId = '';
  activityDeal: any = null;

  page = 1;
  readonly pageSize = 10;
  formatCreatedDate = formatCreatedDate;
  formatReminderDate = formatReminderDate;
  reminderStatus = reminderStatus;
  reminderBadgeClass = reminderBadgeClass;

  /** Sortable columns rendered in the header (in display order). */
  readonly columns: { key: string; label: string; numeric?: boolean; align?: 'right' }[] = [
    { key: 'name', label: 'Engagement' },
    { key: 'customer', label: 'Customer' },
    { key: 'createdAt', label: 'Created' },
    { key: 'expectedCloseDate', label: 'Target Close' },
    { key: 'marketplace', label: 'Marketplace' },
    { key: 'engagementType', label: 'Engagement / Tag' },
    { key: 'marketplaceStatus', label: 'Stage / Status' },
    { key: 'expectedValue', label: 'Value', numeric: true, align: 'right' },
    { key: 'owner', label: 'Owner' }
  ];

  ngOnInit() {
    this.loadDeals();
    this.api.getDealStats().subscribe({
      next: s => this.stats = s,
      error: () => this.stats = null
    });
    // Pick up search + structured filters from query params (top-bar search, Home cards).
    this.route.queryParamMap.subscribe(params => {
      this.search = params.get('q') ?? '';
      this.fOwner = params.get('owner') ?? '';
      this.fStage = params.get('stage') ?? '';
      this.fStatus = params.get('status') ?? '';
      this.fTag = params.get('tag') ?? '';
      this.fScope = params.get('scope') ?? '';
      this.page = 1;
    });
  }

  /** Active filters for the chip row. */
  get activeFilters(): { key: string; label: string }[] {
    const chips: { key: string; label: string }[] = [];
    if (this.fScope === 'open') chips.push({ key: 'scope', label: 'Open only' });
    if (this.fOwner) chips.push({ key: 'owner', label: `Owner: ${this.fOwner}` });
    if (this.fStage) chips.push({ key: 'stage', label: `Stage: ${this.fStage}` });
    if (this.fStatus) chips.push({ key: 'status', label: `Status: ${this.fStatus}` });
    if (this.fTag) chips.push({ key: 'tag', label: `Tag: ${this.fTag}` });
    return chips;
  }

  get hasFilters(): boolean {
    return !!(this.fOwner || this.fStage || this.fStatus || this.fTag || this.fScope);
  }

  removeFilter(key: string) {
    const qp: any = { owner: this.fOwner, stage: this.fStage, status: this.fStatus, tag: this.fTag, scope: this.fScope, q: this.search };
    qp[key] = null;
    this.router.navigate(['/deals'], { queryParams: this.cleanParams(qp) });
  }

  clearAll() {
    this.router.navigate(['/deals'], { queryParams: {} });
  }

  private cleanParams(qp: any) {
    const out: any = {};
    Object.keys(qp).forEach(k => { if (qp[k]) out[k] = qp[k]; });
    return out;
  }

  get filteredDeals(): any[] {
    const q = this.search.trim().toLowerCase();
    let list = this.allDeals;

    if (this.fScope === 'open') list = list.filter(d => this.openStatuses.includes(d.marketplaceStatus));
    if (this.fOwner) list = list.filter(d => (d.owner || '').toLowerCase() === this.fOwner.toLowerCase());
    if (this.fStage) list = list.filter(d => (d.stage || '').toLowerCase() === this.fStage.toLowerCase());
    if (this.fStatus) list = list.filter(d => (d.marketplaceStatus || '').toLowerCase() === this.fStatus.toLowerCase());
    if (this.fTag) list = list.filter(d => (d.campaignEventName || '').toLowerCase() === this.fTag.toLowerCase());

    if (q) {
      list = list.filter(d =>
        (d.id || '').toLowerCase().includes(q) ||
        (d.name || '').toLowerCase().includes(q) ||
        (d.customer || '').toLowerCase().includes(q) ||
        (d.marketplace || '').toLowerCase().includes(q) ||
        (d.stage || '').toLowerCase().includes(q) ||
        (d.owner || '').toLowerCase().includes(q) ||
        (d.engagementType || '').toLowerCase().includes(q) ||
        (d.campaignEventName || '').toLowerCase().includes(q)
      );
    }
    return list;
  }

  /** Filtered list with the active column sort applied. */
  get sortedDeals(): any[] {
    const dir = this.sortDir === 'asc' ? 1 : -1;
    const numeric = this.columns.find(c => c.key === this.sortKey)?.numeric;
    return [...this.filteredDeals].sort((a, b) => {
      if (numeric) {
        return (((+a[this.sortKey] || 0) - (+b[this.sortKey] || 0)) || 0) * dir;
      }
      const va = (a[this.sortKey] ?? '').toString().toLowerCase();
      const vb = (b[this.sortKey] ?? '').toString().toLowerCase();
      // Blank values always sink to the bottom regardless of direction.
      if (!va && vb) return 1;
      if (va && !vb) return -1;
      if (va === vb) return ((a.id || '') < (b.id || '') ? -1 : 1) * dir;
      return (va < vb ? -1 : 1) * dir;
    });
  }

  setSort(key: string) {
    if (this.sortKey === key) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortKey = key;
      // Dates and value feel natural newest/highest-first; text columns A→Z.
      this.sortDir = key === 'createdAt' || key === 'expectedCloseDate' || key === 'expectedValue' ? 'desc' : 'asc';
    }
    this.page = 1;
  }

  sortArrow(key: string): string {
    if (this.sortKey !== key) return '';
    return this.sortDir === 'asc' ? '▲' : '▼';
  }

  get pagedDeals(): any[] {
    return paginateSlice(this.sortedDeals, this.page, this.pageSize);
  }

  get totalPages(): number {
    return totalPages(this.filteredDeals.length, this.pageSize);
  }

  // ---- Date-based flags (fresh / closing soon / overdue) ----
  flagsOf(d: any): EngagementFlag[] { return engagementFlags(d); }
  rowToneOf(d: any): FlagTone | '' { return rowTone(d); }

  // ---- Row action (kebab) menu ----
  toggleMenu(id: string, event: Event) {
    event.stopPropagation();
    this.openMenuId = this.openMenuId === id ? '' : id;
  }
  closeMenu() { this.openMenuId = ''; }

  runAction(fn: () => void) {
    this.openMenuId = '';
    fn();
  }

  // ---- Tasks / Reminders quick-view popup ----
  openActivity(d: any) {
    this.openMenuId = '';
    this.activityDeal = d;
  }
  closeActivity() { this.activityDeal = null; }

  get activityActions(): ActionItemRow[] {
    return normalizeActionItems(this.activityDeal?.meetingNotes?.actionItems ?? []);
  }
  get activityReminders(): ReminderRow[] {
    return normalizeReminders(this.activityDeal?.meetingNotes?.reminders ?? []);
  }

  actionStatusClass(status: string): string {
    switch (status) {
      case 'Done': return 'badge-green';
      case 'In Progress': return 'badge-blue';
      default: return 'badge-orange';
    }
  }

  // ---- Engagement Snapshot over the current filters ----
  private buildSnapshotRequest(): SnapshotRequest {
    return {
      scope: 'filtered',
      owner: this.fOwner || undefined,
      stage: this.fStage || undefined,
      status: this.fStatus || undefined,
      tag: this.fTag || undefined,
      search: this.search.trim() || undefined,
      openOnly: this.fScope === 'open'
    };
  }

  generateSnapshot(email = false) {
    this.launcher.launch(this.buildSnapshotRequest(), { email });
  }

  /** Export the currently-filtered engagements as a CSV file. */
  exportCsv() {
    const rows = this.sortedDeals;
    const headers = ['ID', 'Name', 'Customer', 'Marketplace', 'Engagement Type', 'Tag', 'Stage', 'Status', 'Expected Value', 'Owner', 'Created'];
    const escape = (v: any) => `"${String(v ?? '').replace(/"/g, '""')}"`;
    const lines = [headers.join(',')];
    for (const d of rows) {
      lines.push([
        d.id, d.name, d.customer, d.marketplace, d.engagementType, d.campaignEventName,
        d.stage, d.marketplaceStatus, d.expectedValue, d.owner, d.createdAt
      ].map(escape).join(','));
    }
    const blob = new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `engagements-${rows.length}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  loadDeals() {
    this.api.getDeals(this.view).subscribe({
      next: (d: any) => this.allDeals = Array.isArray(d) ? d : [],
      error: () => this.allDeals = []
    });
  }

  setView(view: 'active' | 'archived') {
    if (this.view === view) return;
    this.view = view;
    this.page = 1;
    this.loadDeals();
  }

  // ---- Archive / Restore / Delete ----
  private countsLine(d: any): string {
    const tasks = d?.meetingNotes?.actionItems?.length || 0;
    const reminders = d?.meetingNotes?.reminders?.length || 0;
    return `It has ${tasks} task(s) and ${reminders} reminder(s).`;
  }

  private label(d: any): string { return d?.name || d?.id || 'this engagement'; }

  async archive(d: any) {
    const ok = await this.confirm.open({
      title: `Archive “${this.label(d)}”?`,
      lines: [
        'Archiving hides this engagement from your dashboards and lists, together with its tasks and reminders.',
        this.countsLine(d),
        'Nothing is deleted — you can restore it anytime from the Archived view.'
      ],
      confirmLabel: 'Archive'
    });
    if (!ok) return;
    this.api.archiveDeal(d.id).subscribe({
      next: (r: any) => this.afterAction(r?.message || 'Engagement archived.'),
      error: () => this.toast = 'Could not archive — is the API running?'
    });
  }

  async restore(d: any) {
    this.api.unarchiveDeal(d.id).subscribe({
      next: (r: any) => this.afterAction(r?.message || 'Engagement restored.'),
      error: () => this.toast = 'Could not restore — is the API running?'
    });
  }

  async remove(d: any) {
    const ok = await this.confirm.open({
      title: `Permanently delete “${this.label(d)}”?`,
      lines: [
        'This permanently removes the engagement and everything attached to it — pricing, meeting notes, tasks, reminders, and full change history.',
        this.countsLine(d),
        'This cannot be undone. The engagement cannot be retrieved once deleted.'
      ],
      confirmLabel: 'Delete permanently',
      danger: true
    });
    if (!ok) return;
    this.api.deleteDeal(d.id).subscribe({
      next: (r: any) => this.afterAction(r?.message || 'Engagement deleted.'),
      error: () => this.toast = 'Could not delete — is the API running?'
    });
  }

  private afterAction(message: string) {
    this.toast = message;
    this.loadDeals();
    this.api.getDealStats().subscribe({ next: s => this.stats = s, error: () => {} });
    setTimeout(() => { if (this.toast === message) this.toast = ''; }, 4000);
  }

  onSearchChange() {
    this.page = 1;
  }

  prevPage() {
    if (this.page > 1) this.page--;
  }

  nextPage() {
    if (this.page < this.totalPages) this.page++;
  }
}
