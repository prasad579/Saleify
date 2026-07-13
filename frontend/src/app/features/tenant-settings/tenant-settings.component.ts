import { Component, OnInit, inject } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

interface CloudCard {
  cloud: 'AWS' | 'Azure' | 'GCP';
  icon: string;
  label: string;
}

@Component({
  selector: 'app-tenant-settings',
  standalone: true,
  imports: [],
  templateUrl: './tenant-settings.component.html',
  styleUrl: './tenant-settings.component.scss'
})
export class TenantSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  tenant: any = null;
  loading = true;
  busyCloud = '';

  readonly clouds: CloudCard[] = [
    { cloud: 'AWS', icon: '🟧', label: 'AWS Marketplace' },
    { cloud: 'Azure', icon: '🟦', label: 'Azure Marketplace' },
    { cloud: 'GCP', icon: '🟩', label: 'Google Cloud Marketplace' }
  ];

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getMyTenant().subscribe({
      next: t => { this.tenant = t; this.loading = false; },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not load tenant.')); this.loading = false; }
    });
  }

  connectionFor(cloud: string): any {
    return this.tenant?.connections?.find((c: any) => c.cloud === cloud) ?? { cloud, status: 'Not Connected' };
  }

  isConnected(cloud: string): boolean {
    return this.connectionFor(cloud).status !== 'Not Connected';
  }

  connect(cloud: string) {
    if (this.busyCloud) return;
    const sellerLabel = window.prompt(`Enter a seller/account label for ${cloud} (mock — any value works):`, `${this.auth.user()?.tenantName || 'my'}-${cloud.toLowerCase()}-seller`);
    if (!sellerLabel) return;
    this.busyCloud = cloud;
    this.api.connectMarketplace(cloud, sellerLabel).subscribe({
      next: res => { this.tenant = res.tenant; this.busyCloud = ''; this.toast.success(`${cloud} connected and synced.`); },
      error: e => { this.busyCloud = ''; this.toast.error(apiErrorMessage(e, `Could not connect ${cloud}.`)); }
    });
  }

  sync(cloud: string) {
    if (this.busyCloud) return;
    this.busyCloud = cloud;
    this.api.syncMarketplace(cloud).subscribe({
      next: res => { this.tenant = res.tenant; this.busyCloud = ''; this.toast.success(`${cloud} re-synced.`); },
      error: e => { this.busyCloud = ''; this.toast.error(apiErrorMessage(e, `Could not sync ${cloud}.`)); }
    });
  }

  disconnect(cloud: string) {
    if (this.busyCloud) return;
    if (!window.confirm(`Disconnect ${cloud}? Its products will be hidden from new engagements (existing engagements keep working).`)) return;
    this.busyCloud = cloud;
    this.api.disconnectMarketplace(cloud).subscribe({
      next: res => { this.tenant = res.tenant; this.busyCloud = ''; this.toast.success(`${cloud} disconnected.`); },
      error: e => { this.busyCloud = ''; this.toast.error(apiErrorMessage(e, `Could not disconnect ${cloud}.`)); }
    });
  }
}
