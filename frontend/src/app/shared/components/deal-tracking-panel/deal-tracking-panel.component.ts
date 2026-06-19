import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  ACTION_STATUSES,
  ActionItemRow,
  ChangeHistoryRow,
  DealTrackingTab,
  HISTORY_CATEGORIES,
  MeetingSessionRow,
  REMINDER_TYPES,
  ReminderRow,
  actionNeedsDate,
  filterBySession,
  filterHistory,
  formatSessionDate,
  getAllSummaryFields,
  meaningfulFields,
  newActionItem,
  newReminder,
  sessionLabel,
  truncateNotes
} from '@shared/utils/meeting-notes.util';
import { paginateSlice, totalPages } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-deal-tracking-panel',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './deal-tracking-panel.component.html',
  styleUrl: './deal-tracking-panel.component.scss'
})
export class DealTrackingPanelComponent {
  @Input() dealId = '';
  @Input() editable = false;
  @Input() saving = false;
  @Input() sessions: MeetingSessionRow[] = [];
  @Input() actionItems: ActionItemRow[] = [];
  @Input() reminders: ReminderRow[] = [];
  @Input() changeHistory: ChangeHistoryRow[] = [];
  @Input() pendingSessionId = '';
  @Input() linkNewItemsToSession = '';
  @Output() linkNewItemsToSessionChange = new EventEmitter<string>();
  @Output() save = new EventEmitter<void>();

  activeTab: DealTrackingTab = 'meetings';
  sessionFilter = 'all';
  expandedSessionId = '';

  sessionsPage = 1;
  actionsPage = 1;
  remindersPage = 1;
  historyPage = 1;
  readonly tablePageSize = 5;

  historyCategory = 'All';
  historySearch = '';
  readonly historyCategories = HISTORY_CATEGORIES;
  readonly actionStatuses = ACTION_STATUSES;
  readonly reminderTypes = REMINDER_TYPES;

  formatSessionDate = formatSessionDate;
  sessionLabel = sessionLabel;
  truncateNotes = truncateNotes;
  getAllSummaryFields = getAllSummaryFields;
  meaningfulFields = meaningfulFields;

  get lastSession(): MeetingSessionRow | null {
    return this.sessions[0] ?? null;
  }

  get filteredActions(): ActionItemRow[] {
    return filterBySession(this.actionItems, this.sessionFilter);
  }

  get filteredReminders(): ReminderRow[] {
    return filterBySession(this.reminders, this.sessionFilter);
  }

  get pagedSessions(): MeetingSessionRow[] {
    return paginateSlice(this.sessions, this.sessionsPage, this.tablePageSize);
  }

  get sessionsTotalPages(): number {
    return totalPages(this.sessions.length, this.tablePageSize);
  }

  get pagedActions(): ActionItemRow[] {
    return paginateSlice(this.filteredActions, this.actionsPage, this.tablePageSize);
  }

  get actionsTotalPages(): number {
    return totalPages(this.filteredActions.length, this.tablePageSize);
  }

  get pagedReminders(): ReminderRow[] {
    return paginateSlice(this.filteredReminders, this.remindersPage, this.tablePageSize);
  }

  get remindersTotalPages(): number {
    return totalPages(this.filteredReminders.length, this.tablePageSize);
  }

  get filteredHistory(): ChangeHistoryRow[] {
    return filterHistory(this.changeHistory, this.historyCategory, this.historySearch);
  }

  get pagedHistory(): ChangeHistoryRow[] {
    return paginateSlice(this.filteredHistory, this.historyPage, this.tablePageSize);
  }

  get historyTotalPages(): number {
    return totalPages(this.filteredHistory.length, this.tablePageSize);
  }

  needsDate(item: ActionItemRow): boolean {
    return actionNeedsDate(item);
  }

  discussionLabel(sessionId?: string): string {
    if (!sessionId) return '—';
    return sessionLabel(this.sessions, sessionId);
  }

  setTab(tab: DealTrackingTab) {
    this.activeTab = tab;
    document.getElementById('deal-tracking-panel')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  toggleSession(id: string) {
    this.expandedSessionId = this.expandedSessionId === id ? '' : id;
  }

  onSessionFilterChange() {
    this.actionsPage = 1;
    this.remindersPage = 1;
  }

  onHistoryFilterChange() {
    this.historyPage = 1;
  }

  onLinkChange(value: string) {
    this.linkNewItemsToSessionChange.emit(value);
  }

  prevPage(which: 'sessions' | 'actions' | 'reminders' | 'history') {
    const map = { sessions: 'sessionsPage', actions: 'actionsPage', reminders: 'remindersPage', history: 'historyPage' } as const;
    const key = map[which];
    if ((this as any)[key] > 1) (this as any)[key]--;
  }

  nextPage(which: 'sessions' | 'actions' | 'reminders' | 'history') {
    const totals = {
      sessions: this.sessionsTotalPages,
      actions: this.actionsTotalPages,
      reminders: this.remindersTotalPages,
      history: this.historyTotalPages
    };
    const map = { sessions: 'sessionsPage', actions: 'actionsPage', reminders: 'remindersPage', history: 'historyPage' } as const;
    const key = map[which];
    if ((this as any)[key] < totals[which]) (this as any)[key]++;
  }

  addActionItem() {
    const sid = this.linkNewItemsToSession || this.pendingSessionId || '';
    this.actionItems.push(newActionItem(sid));
  }

  removeActionItem(id: string) {
    const idx = this.actionItems.findIndex(a => a.id === id);
    if (idx >= 0) this.actionItems.splice(idx, 1);
  }

  addReminder() {
    const sid = this.linkNewItemsToSession || this.pendingSessionId || '';
    this.reminders.push(newReminder(sid));
  }

  removeReminder(id: string) {
    const idx = this.reminders.findIndex(r => r.id === id);
    if (idx >= 0) this.reminders.splice(idx, 1);
  }
}
