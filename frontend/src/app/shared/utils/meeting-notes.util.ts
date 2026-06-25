export interface SummaryField {
  key: string;
  label: string;
  value: string;
  dynamic?: boolean;
}

export interface ExtractedSummary {
  standardFields: SummaryField[];
  dynamicFields: SummaryField[];
}

export interface ActionItemRow {
  id: string;
  task: string;
  owner: string;
  dueDate: string;
  dueDateHint: string;
  status: string;
  source?: string;
  sessionId?: string;
}

export interface ReminderRow {
  id: string;
  reminder: string;
  /** The "remind on" / due date (yyyy-MM-dd). */
  dateTime: string;
  type: string;
  /** When the reminder was created (yyyy-MM-dd). */
  createdAt: string;
  sessionId?: string;
}

export interface MeetingSessionRow {
  id: string;
  title: string;
  rawNotes: string;
  extracted: ExtractedSummary | null;
  createdAt: string;
}

export interface ChangeHistoryRow {
  id: string;
  timestamp: string;
  category: string;
  summary: string;
  details: string;
  changedBy: string;
}

export type DealTrackingTab = 'meetings' | 'actions' | 'reminders' | 'history';

const LEGACY_STANDARD: { key: string; label: string; prop: string }[] = [
  { key: 'contractDuration', label: 'Contract duration', prop: 'contractDuration' },
  { key: 'discountRequested', label: 'Discount requested', prop: 'discountRequested' },
  { key: 'paymentModel', label: 'Payment model', prop: 'paymentModel' },
  { key: 'legalReview', label: 'Legal review', prop: 'legalReview' },
  { key: 'customerInterest', label: 'Customer interest', prop: 'customerInterest' }
];

export const ACTION_STATUSES = ['Pending', 'In Progress', 'Done'];
export const REMINDER_TYPES = ['Follow-up', 'Meeting', 'Call', 'Other'];

export function normalizeExtracted(raw: any): ExtractedSummary | null {
  if (!raw) return null;

  if (raw.standardFields?.length || raw.dynamicFields?.length) {
    return {
      standardFields: (raw.standardFields || []).map(mapField),
      dynamicFields: (raw.dynamicFields || []).map(mapField)
    };
  }

  return {
    standardFields: LEGACY_STANDARD.map(({ key, label, prop }) => ({
      key,
      label,
      value: raw[prop] || 'Not specified'
    })),
    dynamicFields: []
  };
}

function mapField(f: any): SummaryField {
  return {
    key: f.key || '',
    label: f.label || f.key || 'Field',
    value: f.value || '—'
  };
}

/** All summary fields for display — template + dynamic from AI. */
export function getAllSummaryFields(extracted: ExtractedSummary | null): SummaryField[] {
  if (!extracted) return [];
  const standard = (extracted.standardFields || []).map(f => ({ ...f, dynamic: false }));
  const dynamic = (extracted.dynamicFields || []).map(f => ({ ...f, dynamic: true }));
  return [...standard, ...dynamic].filter(f => f.value && f.value !== '—');
}

export function meaningfulFields(fields: SummaryField[]): SummaryField[] {
  return fields.filter(f => {
    const v = (f.value || '').toLowerCase();
    return v && !v.includes('not specified') && !v.includes('not mentioned');
  });
}

export function normalizeActionItems(items: any[]): ActionItemRow[] {
  return (items || []).map(a => ({
    id: a.id || newId(),
    task: a.task || '',
    owner: a.owner || 'Srinivas K',
    dueDate: normalizeDueDate(a.dueDate),
    dueDateHint: a.dueDateHint || legacyDueHint(a.dueDate),
    status: a.status || 'Pending',
    source: a.source || 'ai',
    sessionId: a.sessionId || ''
  }));
}

export function normalizeReminders(items: any[]): ReminderRow[] {
  const rows = (items || []).map(r => ({
    id: r.id || newId(),
    reminder: r.reminder || '',
    dateTime: r.dateTime || '',
    type: r.type || 'Follow-up',
    createdAt: r.createdAt || '',
    sessionId: r.sessionId || ''
  }));
  return sortRemindersByDue(rows);
}

/** Soonest first; overdue at the top; reminders with no/unknown date last. */
export function sortRemindersByDue(rows: ReminderRow[]): ReminderRow[] {
  return [...rows].sort((a, b) => {
    const da = isoDateValue(a.dateTime);
    const db = isoDateValue(b.dateTime);
    if (da === null && db === null) return 0;
    if (da === null) return 1;
    if (db === null) return -1;
    return da - db;
  });
}

function isoDateValue(value: string): number | null {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const t = new Date(value + 'T00:00:00').getTime();
  return Number.isNaN(t) ? null : t;
}

export type ReminderTone = 'overdue' | 'today' | 'soon' | 'upcoming' | 'none';

export interface ReminderStatus {
  label: string;
  tone: ReminderTone;
  /** Days until due (negative = overdue), or null when there's no valid date. */
  days: number | null;
}

/** Status of a reminder relative to today: Overdue / Due today / Due in N days / Upcoming. */
export function reminderStatus(dueIso: string): ReminderStatus {
  const due = isoDateValue(dueIso);
  if (due === null) return { label: 'No date set', tone: 'none', days: null };
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const days = Math.round((due - today.getTime()) / 86_400_000);
  if (days < 0) return { label: `Overdue by ${Math.abs(days)} day${days === -1 ? '' : 's'}`, tone: 'overdue', days };
  if (days === 0) return { label: 'Due today', tone: 'today', days };
  if (days <= 3) return { label: `Due in ${days} day${days === 1 ? '' : 's'}`, tone: 'soon', days };
  return { label: `Due in ${days} days`, tone: 'upcoming', days };
}

/** Maps a reminder tone to a global badge class. */
export function reminderBadgeClass(tone: ReminderTone): string {
  switch (tone) {
    case 'overdue': return 'badge-red';
    case 'today': return 'badge-orange';
    case 'soon': return 'badge-orange';
    case 'upcoming': return 'badge-blue';
    default: return 'badge-gray';
  }
}

/** Render an ISO date as "02 Jul 2026"; pass through non-ISO legacy strings. */
export function formatReminderDate(iso: string): string {
  if (!iso) return '—';
  if (/^\d{4}-\d{2}-\d{2}$/.test(iso)) {
    const d = new Date(iso + 'T00:00:00');
    if (!Number.isNaN(d.getTime())) {
      return d.toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' });
    }
  }
  return iso;
}

export function normalizeSessions(sessions: any[], legacyNotes?: any): MeetingSessionRow[] {
  const mapped = sessions?.length
    ? sessions.map(s => ({
        id: s.id || newId(),
        title: s.title || 'Meeting notes',
        rawNotes: s.rawNotes || '',
        extracted: normalizeExtracted(s.extracted),
        createdAt: s.createdAt || ''
      }))
    : legacyNotes?.rawNotes
      ? [{
          id: newId(),
          title: 'Initial meeting',
          rawNotes: legacyNotes.rawNotes,
          extracted: normalizeExtracted(legacyNotes.extracted),
          createdAt: legacyNotes.createdAt || ''
        }]
      : [];

  return mapped.sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''));
}

export function normalizeHistory(items: any[]): ChangeHistoryRow[] {
  return (items || []).map(h => ({
    id: h.id || newId(),
    timestamp: h.timestamp || '',
    category: h.category || '',
    summary: h.summary || '',
    details: h.details || '',
    changedBy: h.changedBy || ''
  }));
}

export function mergeActionItems(
  existing: ActionItemRow[],
  suggested: ActionItemRow[],
  sessionId?: string
): ActionItemRow[] {
  const merged = [...existing];
  const seen = new Set(merged.map(i => i.task.trim().toLowerCase()));
  for (const item of suggested) {
    const key = item.task.trim().toLowerCase();
    if (!key || seen.has(key)) continue;
    seen.add(key);
    merged.push({
      ...item,
      id: newId(),
      source: 'ai',
      sessionId: item.sessionId || sessionId || ''
    });
  }
  return merged;
}

export function newActionItem(sessionId = ''): ActionItemRow {
  return {
    id: newId(),
    task: '',
    owner: 'Srinivas K',
    dueDate: '',
    dueDateHint: '',
    status: 'Pending',
    source: 'manual',
    sessionId
  };
}

export function newReminder(sessionId = ''): ReminderRow {
  const today = new Date().toISOString().slice(0, 10);
  return {
    id: newId(),
    reminder: '',
    dateTime: today,
    type: 'Follow-up',
    createdAt: today,
    sessionId
  };
}

export function sessionLabel(sessions: MeetingSessionRow[], sessionId?: string): string {
  if (!sessionId) return '—';
  const s = sessions.find(x => x.id === sessionId);
  return s ? s.title : sessionId;
}

function legacyDueHint(due: string): string {
  if (!due || /^\d{4}-\d{2}-\d{2}$/.test(due)) return '';
  return `${due} (from notes — pick a date)`;
}

function normalizeDueDate(due: string): string {
  if (!due) return '';
  if (/^\d{4}-\d{2}-\d{2}$/.test(due)) return due;
  return '';
}

export function actionNeedsDate(item: ActionItemRow): boolean {
  return !item.dueDate;
}

export function formatSessionDate(iso: string): string {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
}

export function getLastSession(deal: any): MeetingSessionRow | null {
  const saved = deal?.meetingNotes;
  const sessions = normalizeSessions(saved?.sessions, saved);
  return sessions[0] ?? null;
}

export interface SnapshotHighlight {
  label: string;
  value: string;
}

export function getSnapshotHighlights(session: MeetingSessionRow | null, max = 5): SnapshotHighlight[] {
  return meaningfulFields(getAllSummaryFields(session?.extracted ?? null))
    .slice(0, max)
    .map(f => ({ label: f.label, value: f.value }));
}

export function truncateNotes(text: string, max = 220): string {
  if (!text) return '';
  const t = text.trim().replace(/\s+/g, ' ');
  if (t.length <= max) return t;
  return t.slice(0, max).trim() + '…';
}

export const HISTORY_CATEGORIES = ['All', 'Deal', 'Products', 'Pricing', 'Meeting Notes', 'Approvals'];

export function filterHistory(
  items: ChangeHistoryRow[],
  category: string,
  search: string
): ChangeHistoryRow[] {
  const q = search.trim().toLowerCase();
  return items.filter(h => {
    if (category !== 'All' && h.category !== category) return false;
    if (!q) return true;
    return (
      h.summary.toLowerCase().includes(q) ||
      h.details.toLowerCase().includes(q) ||
      h.category.toLowerCase().includes(q) ||
      h.changedBy.toLowerCase().includes(q)
    );
  });
}

export function filterBySession<T extends { sessionId?: string }>(
  items: T[],
  sessionFilter: string
): T[] {
  if (sessionFilter === 'all') return items;
  if (sessionFilter === 'none') return items.filter(i => !i.sessionId);
  return items.filter(i => i.sessionId === sessionFilter);
}

export function newId(): string {
  return crypto.randomUUID().slice(0, 8);
}
