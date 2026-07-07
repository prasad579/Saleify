import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import { paginateSlice, totalPages } from '@shared/utils/pagination.util';

@Component({
  selector: 'app-engagement-requests-list',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './engagement-requests-list.component.html',
  styleUrl: './engagement-requests-list.component.scss'
})
export class EngagementRequestsListComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);

  requests: any[] = [];
  loading = true;
  search = '';
  page = 1;
  pageSize = 8;

  ngOnInit() {
    const email = this.auth.user()?.email;
    if (!email) return;
    this.api.getMyEngagementRequests(email).subscribe({
      next: (list) => { this.requests = list || []; this.loading = false; },
      error: () => { this.requests = []; this.loading = false; }
    });
  }

  get filtered() {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.requests;
    return this.requests.filter(r =>
      (r.id || '').toLowerCase().includes(q) ||
      (r.requestType || '').toLowerCase().includes(q) ||
      (r.marketplace || '').toLowerCase().includes(q) ||
      (r.status || '').toLowerCase().includes(q)
    );
  }

  get pageItems() {
    return paginateSlice(this.filtered, this.page, this.pageSize);
  }

  get totalPageCount() {
    return totalPages(this.filtered.length, this.pageSize);
  }

  onSearchChange() {
    this.page = 1;
  }

  statusClass(status: string) {
    switch ((status || '').toLowerCase()) {
      case 'accepted': return 'badge-green';
      case 'declined': return 'badge-red';
      case 'under review':
      case 'in progress': return 'badge-orange';
      default: return 'badge-blue';
    }
  }

  newRequest() {
    void this.router.navigate(['/portal/requests/new']);
  }
}
