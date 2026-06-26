import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { SnapshotLauncherService } from '@core/services/snapshot-launcher.service';
import { SnapshotSettingsService } from '@core/services/snapshot-settings.service';
import { ToastService } from '@core/services/toast.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { CampaignEvent, eventStatus, MARKETPLACES } from '@shared/data/lookups';

interface FunnelStage { label: string; count: number; }
interface ConversionFunnel {
  eventId: string;
  eventName: string;
  totalDeals: number;
  closedWon: number;
  stages: FunnelStage[];
}

@Component({
  selector: 'app-campaign-events',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './campaign-events.component.html',
  styleUrl: './campaign-events.component.scss'
})
export class CampaignEventsComponent implements OnInit {
  private api = inject(ApiService);
  private launcher = inject(SnapshotLauncherService);
  private toast = inject(ToastService);
  snapSettings = inject(SnapshotSettingsService);

  marketplaces = MARKETPLACES;
  events: CampaignEvent[] = [];
  search = '';
  loading = true;
  error = '';
  success = '';

  get filteredEvents(): CampaignEvent[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.events;
    return this.events.filter(ev =>
      (ev.name || '').toLowerCase().includes(q) ||
      (ev.marketplace || '').toLowerCase().includes(q) ||
      (ev.description || '').toLowerCase().includes(q) ||
      this.statusOf(ev).toLowerCase().includes(q)
    );
  }

  showForm = false;
  editId = '';
  form = { name: '', marketplace: 'AWS', startDate: '', endDate: '', description: '' };

  conversionFor = '';
  conversion: ConversionFunnel | null = null;

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getCampaignEvents().subscribe({
      next: (data: any) => {
        this.events = Array.isArray(data) ? data : [];
        this.loading = false;
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not load campaign events.');
        this.loading = false;
      }
    });
  }

  statusOf(ev: CampaignEvent): string {
    return ev.status || eventStatus(ev.startDate, ev.endDate);
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Active': return 'badge-green';
      case 'Upcoming': return 'badge-blue';
      case 'Completed': return 'badge-orange';
      default: return 'badge-gray';
    }
  }

  newEvent() {
    this.showForm = true;
    this.editId = '';
    this.form = { name: '', marketplace: 'AWS', startDate: '', endDate: '', description: '' };
  }

  edit(ev: CampaignEvent) {
    this.showForm = true;
    this.editId = ev.id;
    this.form = {
      name: ev.name,
      marketplace: ev.marketplace,
      startDate: ev.startDate,
      endDate: ev.endDate,
      description: ev.description
    };
  }

  cancel() { this.showForm = false; this.editId = ''; }

  save() {
    this.error = '';
    if (!this.form.name.trim()) { this.toast.error('Event name is required.'); return; }
    if (!this.form.startDate || !this.form.endDate) { this.toast.error('Start and end dates are required.'); return; }
    if (this.form.endDate < this.form.startDate) { this.toast.error('End date must be on or after the start date.'); return; }

    const editing = !!this.editId;
    const req$ = editing
      ? this.api.updateCampaignEvent(this.editId, this.form)
      : this.api.createCampaignEvent(this.form);

    req$.subscribe({
      next: () => {
        this.toast.success(editing ? 'Event updated.' : 'Event created.');
        this.showForm = false;
        this.editId = '';
        this.load();
      },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not save event.')); }
    });
  }

  remove(ev: CampaignEvent) {
    this.api.deleteCampaignEvent(ev.id).subscribe({
      next: () => { this.toast.success('Event deleted.'); this.load(); },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not delete event.')); }
    });
  }

  togglePause(ev: CampaignEvent) {
    this.error = '';
    this.api.toggleCampaignEventPause(ev.id).subscribe({
      next: (updated: any) => {
        this.toast.success(updated?.paused ? `“${ev.name}” paused — hidden from the engagement tag dropdown.` : `“${ev.name}” resumed.`);
        this.load();
      },
      error: (err) => { this.toast.error(apiErrorMessage(err, 'Could not update the tag.')); }
    });
  }

  toggleConversion(ev: CampaignEvent) {
    if (this.conversionFor === ev.id) {
      this.conversionFor = '';
      this.conversion = null;
      return;
    }
    this.conversionFor = ev.id;
    this.conversion = null;
    this.api.getCampaignConversion(ev.id).subscribe({
      next: (data: any) => { this.conversion = data; },
      error: (err) => { this.error = apiErrorMessage(err, 'Could not load conversion data.'); }
    });
  }

  snapshot(ev: CampaignEvent, email = false) {
    this.launcher.launch({ scope: 'event', eventId: ev.id }, { email });
  }

  stagePercent(count: number): number {
    const max = this.conversion?.stages?.[0]?.count || 0;
    if (!max) return 0;
    return Math.round((count / max) * 100);
  }
}
