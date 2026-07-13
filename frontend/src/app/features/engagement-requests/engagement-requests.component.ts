import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { paginateSlice, totalPages } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-engagement-requests',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './engagement-requests.component.html',
  styleUrl: './engagement-requests.component.scss'
})
export class EngagementRequestsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);

  requests: any[] = [];
  loading = true;
  converting = '';
  search = '';
  page = 1;
  readonly pageSize = 10;

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getAllEngagementRequests().subscribe({
      next: list => { this.requests = Array.isArray(list) ? list : []; this.loading = false; },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not load engagement requests.')); this.loading = false; }
    });
  }

  get filtered(): any[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.requests;
    return this.requests.filter(r =>
      (r.id || '').toLowerCase().includes(q) ||
      (r.requestType || '').toLowerCase().includes(q) ||
      (r.marketplace || '').toLowerCase().includes(q) ||
      (r.customerName || '').toLowerCase().includes(q) ||
      (r.companyName || '').toLowerCase().includes(q) ||
      (r.status || '').toLowerCase().includes(q)
    );
  }

  get pageItems() { return paginateSlice(this.filtered, this.page, this.pageSize); }
  get totalPageCount() { return totalPages(this.filtered.length, this.pageSize); }

  onSearchChange() { this.page = 1; }

  statusClass(status: string): string {
    switch ((status || '').toLowerCase()) {
      case 'converted': return 'badge-blue';
      case 'accepted': return 'badge-green';
      case 'declined': return 'badge-red';
      case 'under review':
      case 'in progress': return 'badge-orange';
      default: return 'badge-gray';
    }
  }

  convert(r: any) {
    if (r.status === 'Converted' || this.converting) return;
    this.converting = r.id;
    const owner = this.auth.user()?.name || 'Srinivas K';
    this.api.convertEngagementRequestToDeal(r.id, owner).subscribe({
      next: res => {
        this.converting = '';
        this.toast.success(`Converted to ${res.deal.id}.`);
        void this.router.navigate(['/deals', res.deal.id]);
      },
      error: e => {
        this.converting = '';
        this.toast.error(apiErrorMessage(e, 'Could not convert this request.'));
      }
    });
  }
}
