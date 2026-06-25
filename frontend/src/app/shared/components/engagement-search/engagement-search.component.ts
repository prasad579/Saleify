import { Component, Input, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';

interface QuickFilter { label: string; icon: string; params: Record<string, string>; }

@Component({
  selector: 'app-engagement-search',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './engagement-search.component.html',
  styleUrl: './engagement-search.component.scss'
})
export class EngagementSearchComponent implements OnInit {
  private api = inject(ApiService);
  private router = inject(Router);
  private auth = inject(AuthService);

  @Input() placeholder = 'Search engagements, customers, tags…';

  term = '';
  open = false;
  private deals: any[] = [];

  quickFilters: QuickFilter[] = [];

  ngOnInit() {
    this.api.getDeals().subscribe((d: any) => { this.deals = Array.isArray(d) ? d : []; });
    const me = this.auth.user()?.name || 'Srinivas K';
    this.quickFilters = [
      { label: 'My open engagements', icon: '📂', params: { owner: me, scope: 'open' } },
      { label: 'Approval pending', icon: '⏳', params: { stage: 'Approval' } },
      { label: 'Drafts', icon: '📝', params: { status: 'Draft' } },
      { label: 'Quick captures', icon: '⚡', params: { stage: 'Quick Capture' } },
      { label: 'AWS', icon: '☁️', params: { q: 'AWS' } },
      { label: 'Azure', icon: '☁️', params: { q: 'Azure' } },
      { label: 'GCP', icon: '☁️', params: { q: 'GCP' } }
    ];
  }

  get suggestions(): any[] {
    const q = this.term.trim().toLowerCase();
    if (!q) return [];
    return this.deals.filter(d =>
      (d.id || '').toLowerCase().includes(q) ||
      (d.name || '').toLowerCase().includes(q) ||
      (d.customer || '').toLowerCase().includes(q) ||
      (d.campaignEventName || '').toLowerCase().includes(q) ||
      (d.owner || '').toLowerCase().includes(q) ||
      (d.engagementType || '').toLowerCase().includes(q)
    ).slice(0, 7);
  }

  onFocus() { this.open = true; }
  onBlur() { setTimeout(() => this.open = false, 150); }

  pick(deal: any) {
    this.open = false;
    this.term = '';
    this.router.navigate(['/deals', deal.id]);
  }

  applyFilter(f: QuickFilter) {
    this.open = false;
    this.router.navigate(['/deals'], { queryParams: f.params });
  }

  submit() {
    const q = this.term.trim();
    this.open = false;
    this.router.navigate(['/deals'], { queryParams: q ? { q } : {} });
  }
}
