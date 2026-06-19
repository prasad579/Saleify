export type SortOrder = 'newest' | 'oldest';

export function sortByCreatedAt<T extends { createdAt?: string; id?: string }>(
  items: T[],
  order: SortOrder
): T[] {
  return [...items].sort((a, b) => {
    const da = a.createdAt || '';
    const db = b.createdAt || '';
    const cmp = db.localeCompare(da);
    if (cmp !== 0) return order === 'newest' ? cmp : -cmp;
    return order === 'newest'
      ? (b.id || '').localeCompare(a.id || '')
      : (a.id || '').localeCompare(b.id || '');
  });
}

export function paginateSlice<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}

export function totalPages(count: number, pageSize: number): number {
  return Math.max(1, Math.ceil(count / pageSize));
}

export function formatCreatedDate(iso: string): string {
  if (!iso) return '—';
  const d = new Date(iso + 'T00:00:00');
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}
