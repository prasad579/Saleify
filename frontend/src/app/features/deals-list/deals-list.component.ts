import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, dealContinuePath } from '@core/services/api.service';
import { formatCreatedDate, paginateSlice, sortByCreatedAt, totalPages } from '@shared/utils/pagination.util';
import { SortOrder } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-deals-list',
  standalone: true,
  imports: [RouterLink, DecimalPipe, FormsModule],
  templateUrl: './deals-list.component.html',
  styleUrl: './deals-list.component.scss'
})
export class DealsListComponent implements OnInit {
  private api = inject(ApiService);
  allDeals: any[] = [];
  stats: any = null;
  continuePath = dealContinuePath;

  search = '';
  sortOrder: SortOrder = 'newest';
  page = 1;
  readonly pageSize = 10;
  formatCreatedDate = formatCreatedDate;

  ngOnInit() {
    this.api.getDeals().subscribe({
      next: (d: any) => this.allDeals = Array.isArray(d) ? d : [],
      error: () => this.allDeals = []
    });
    this.api.getDealStats().subscribe({
      next: s => this.stats = s,
      error: () => this.stats = null
    });
  }

  get filteredDeals(): any[] {
    const q = this.search.trim().toLowerCase();
    let list = this.allDeals;
    if (q) {
      list = list.filter(d =>
        (d.id || '').toLowerCase().includes(q) ||
        (d.name || '').toLowerCase().includes(q) ||
        (d.customer || '').toLowerCase().includes(q) ||
        (d.marketplace || '').toLowerCase().includes(q) ||
        (d.stage || '').toLowerCase().includes(q) ||
        (d.owner || '').toLowerCase().includes(q)
      );
    }
    return sortByCreatedAt(list, this.sortOrder);
  }

  get pagedDeals(): any[] {
    return paginateSlice(this.filteredDeals, this.page, this.pageSize);
  }

  get totalPages(): number {
    return totalPages(this.filteredDeals.length, this.pageSize);
  }

  onSearchChange() {
    this.page = 1;
  }

  onSortChange() {
    this.page = 1;
  }

  prevPage() {
    if (this.page > 1) this.page--;
  }

  nextPage() {
    if (this.page < this.totalPages) this.page++;
  }
}
