import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { AuditEntry, AuditLogPage } from '@shared/data/audit.model';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  private api = inject(ApiService);

  page: AuditLogPage | null = null;
  loading = true;
  error = '';

  category = 'all';
  search = '';
  currentPage = 1;
  pageSize = 25;

  /** Categories seen so far — kept across filters so the dropdown doesn't empty out. */
  categories: string[] = [];

  private searchTimer: any = null;

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getAuditLog({
      category: this.category === 'all' ? undefined : this.category,
      search: this.search.trim() || undefined,
      page: this.currentPage,
      pageSize: this.pageSize
    }).subscribe({
      next: p => {
        this.page = p;
        if (p.categories?.length) this.categories = p.categories;
        this.loading = false;
      },
      error: e => { this.error = apiErrorMessage(e, 'Could not load the audit log.'); this.loading = false; }
    });
  }

  onFilterChange() {
    this.currentPage = 1;
    this.load();
  }

  onSearchChange() {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.onFilterChange(), 300);
  }

  prevPage() {
    if (this.currentPage > 1) { this.currentPage--; this.load(); }
  }

  nextPage() {
    if (this.page && this.currentPage * this.pageSize < this.page.total) { this.currentPage++; this.load(); }
  }

  get rangeStart(): number {
    return this.page && this.page.total > 0 ? (this.currentPage - 1) * this.pageSize + 1 : 0;
  }

  get rangeEnd(): number {
    if (!this.page) return 0;
    return Math.min(this.currentPage * this.pageSize, this.page.total);
  }

  get hasNext(): boolean {
    return !!this.page && this.currentPage * this.pageSize < this.page.total;
  }

  /** A deal-scoped entry links to its engagement overview. */
  dealLink(e: AuditEntry): string | null {
    return /^DL-\d+/i.test(e.entityId) ? `/deals/${e.entityId}` : null;
  }

  badgeClass(category: string): string {
    const c = (category || '').toLowerCase();
    if (c === 'settings') return 'badge-purple';
    if (c === 'approvals' || c === 'approval') return 'badge-green';
    if (c === 'pricing') return 'badge-orange';
    if (c === 'products') return 'badge-blue';
    if (c === 'engagement') return 'badge-indigo';
    return 'badge-grey';
  }
}
