import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { ToastService } from '@core/services/toast.service';
import { OfferRequest } from '@shared/data/offer-request.model';
import { offerStatusBadge, offerResponseBadge, offerResponseLabel } from '@shared/utils/offer-request.util';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { formatCreatedDate } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-offer-requests',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './offer-requests.component.html',
  styleUrl: './offer-requests.component.scss'
})
export class OfferRequestsComponent implements OnInit {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private router = inject(Router);

  all: OfferRequest[] = [];
  loading = true;
  formatCreatedDate = formatCreatedDate;
  statusBadge = offerStatusBadge;
  responseBadge = offerResponseBadge;
  responseLabel = offerResponseLabel;

  // ---- Filters ----
  search = '';
  dateFrom = '';
  dateTo = '';
  filters: { field: string; value: string }[] = [];
  newFilterField = '';
  newFilterValue = '';

  readonly filterFields: { key: string; label: string }[] = [
    { key: 'engagementType', label: 'Engagement Type' },
    { key: 'marketplace', label: 'Marketplace' },
    { key: 'product', label: 'Product' },
    { key: 'status', label: 'Stage / Status' },
    { key: 'destination', label: 'Destination' },
    { key: 'response', label: 'Response' }
  ];

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getOfferRequests().subscribe({
      next: list => { this.all = Array.isArray(list) ? list : []; this.loading = false; },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not load offer requests.')); this.loading = false; }
    });
  }

  open(o: OfferRequest) { this.router.navigate(['/offer-requests', o.id]); }

  // ---- Filtering ----
  fieldLabel(key: string): string {
    return this.filterFields.find(f => f.key === key)?.label ?? key;
  }

  private valueOf(o: OfferRequest, field: string): string {
    if (field === 'response') return this.responseLabel(o);
    return ((o as any)[field] ?? '').toString();
  }

  valueOptionsFor(field: string): string[] {
    if (!field) return [];
    const vals = new Set<string>();
    for (const o of this.all) {
      if (field === 'product') {
        for (const p of (o.products || [])) { if (p) vals.add(p); }
      } else {
        const v = this.valueOf(o, field).trim();
        if (v) vals.add(v);
      }
    }
    return [...vals].sort((a, b) => a.localeCompare(b));
  }

  get newFilterValueOptions(): string[] { return this.valueOptionsFor(this.newFilterField); }
  onFilterFieldChange() { this.newFilterValue = ''; }

  addFilter() {
    if (!this.newFilterField || !this.newFilterValue) return;
    if (!this.filters.some(f => f.field === this.newFilterField && f.value === this.newFilterValue)) {
      this.filters = [...this.filters, { field: this.newFilterField, value: this.newFilterValue }];
    }
    this.newFilterValue = '';
  }

  removeFilterRule(i: number) { this.filters = this.filters.filter((_, idx) => idx !== i); }

  get hasFilters(): boolean {
    return this.filters.length > 0 || !!this.dateFrom || !!this.dateTo || !!this.search.trim();
  }

  clearAll() {
    this.filters = [];
    this.dateFrom = '';
    this.dateTo = '';
    this.search = '';
  }

  /** "2026-06-25 16:06:02 UTC" → "2026-06-25" for date-range comparison. */
  private dateKey(ts: string): string { return (ts || '').slice(0, 10); }

  get filtered(): OfferRequest[] {
    const q = this.search.trim().toLowerCase();
    let list = this.all;

    for (const f of this.filters) {
      const val = f.value.toLowerCase();
      if (f.field === 'product') {
        list = list.filter(o => (o.products || []).some(p => p.toLowerCase() === val));
      } else {
        list = list.filter(o => this.valueOf(o, f.field).toLowerCase() === val);
      }
    }

    if (this.dateFrom) list = list.filter(o => this.dateKey(o.submittedAt) >= this.dateFrom);
    if (this.dateTo) list = list.filter(o => this.dateKey(o.submittedAt) <= this.dateTo);

    if (q) {
      list = list.filter(o =>
        (o.id || '').toLowerCase().includes(q) ||
        (o.engagementName || '').toLowerCase().includes(q) ||
        (o.customer || '').toLowerCase().includes(q) ||
        (o.dealId || '').toLowerCase().includes(q) ||
        (o.marketplace || '').toLowerCase().includes(q) ||
        (o.destination || '').toLowerCase().includes(q) ||
        (o.products || []).join(' ').toLowerCase().includes(q)
      );
    }
    return list;
  }

  // ---- Summary counts ----
  get total(): number { return this.all.length; }
  get awaitingCount(): number { return this.all.filter(o => !o.responseReceived).length; }
  get acceptedCount(): number { return this.all.filter(o => o.responseStatus === 'Accepted').length; }
  get rejectedCount(): number { return this.all.filter(o => o.responseStatus === 'Rejected').length; }
}
