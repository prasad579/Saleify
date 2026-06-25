/**
 * Lifecycle "freshness / deadline" flags for an engagement, derived from its
 * created date vs. target close date, so the list view can highlight records
 * that are brand new or approaching / past their close date.
 */

export type FlagTone = 'fresh' | 'soon' | 'overdue';

export interface EngagementFlag {
  icon: string;
  label: string;
  tone: FlagTone;
  /** Tooltip with the detail behind the badge. */
  title: string;
  /** Global badge class. */
  badgeClass: string;
}

/** Engagements in these statuses are closed — no freshness/deadline flags apply. */
const CLOSED_STATUSES = ['Published', 'Abandoned', 'Completed'];

/** Days from today for a yyyy-MM-dd date. Negative = in the past, null = no/!ISO date. */
function daysFromToday(iso: string | undefined | null): number | null {
  if (!iso || !/^\d{4}-\d{2}-\d{2}$/.test(iso)) return null;
  const t = new Date(iso + 'T00:00:00').getTime();
  if (Number.isNaN(t)) return null;
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return Math.round((t - today.getTime()) / 86_400_000);
}

const FRESH_WINDOW_DAYS = 7;
const CLOSING_WINDOW_DAYS = 7;

export function engagementFlags(deal: any): EngagementFlag[] {
  const flags: EngagementFlag[] = [];
  if (!deal) return flags;
  const closed = CLOSED_STATUSES.includes(deal.marketplaceStatus);
  if (closed) return flags;

  // Fresh — created within the last week.
  const createdDays = daysFromToday(deal.createdAt);
  if (createdDays !== null && createdDays <= 0 && createdDays >= -FRESH_WINDOW_DAYS) {
    const ago = -createdDays;
    flags.push({
      icon: '🌱',
      label: 'New',
      tone: 'fresh',
      title: ago === 0 ? 'Created today' : `Created ${ago} day${ago === 1 ? '' : 's'} ago`,
      badgeClass: 'badge-green'
    });
  }

  // Deadline — target close date in the past (overdue) or within the next week (closing soon).
  const dueDays = daysFromToday(deal.expectedCloseDate);
  if (dueDays !== null) {
    if (dueDays < 0) {
      const by = -dueDays;
      flags.push({
        icon: '⚠️',
        label: `Overdue ${by}d`,
        tone: 'overdue',
        title: `Target close date passed ${by} day${by === 1 ? '' : 's'} ago`,
        badgeClass: 'badge-red'
      });
    } else if (dueDays <= CLOSING_WINDOW_DAYS) {
      flags.push({
        icon: '⏰',
        label: dueDays === 0 ? 'Closes today' : `Closes in ${dueDays}d`,
        tone: 'soon',
        title: 'Target close date is approaching',
        badgeClass: 'badge-orange'
      });
    }
  }

  return flags;
}

/** The strongest tone for a whole-row accent: overdue beats closing-soon beats fresh. */
export function rowTone(deal: any): FlagTone | '' {
  const flags = engagementFlags(deal);
  if (flags.some(f => f.tone === 'overdue')) return 'overdue';
  if (flags.some(f => f.tone === 'soon')) return 'soon';
  if (flags.some(f => f.tone === 'fresh')) return 'fresh';
  return '';
}
