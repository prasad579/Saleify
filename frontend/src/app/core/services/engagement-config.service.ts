import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '@environments/environment';
import { EngagementTypeSettings } from '@shared/data/engagement-types.model';
import { EngagementTypeConfig, applyEngagementConfigs } from '@shared/utils/engagement.util';

/**
 * Loads the tenant's engagement-type catalog from the API at app startup and applies it to the
 * shared engagement config (so the create picker, stepper, and flow honor Settings → Engagement
 * Types). On failure it leaves the built-in defaults in place so the app still works offline.
 */
@Injectable({ providedIn: 'root' })
export class EngagementConfigService {
  private http = inject(HttpClient);

  async load(): Promise<void> {
    try {
      const settings = await firstValueFrom(
        this.http.get<EngagementTypeSettings>(`${environment.apiUrl}/engagement-types`)
      );
      this.applySettings(settings);
    } catch {
      // Keep the built-in defaults — the app remains usable if the API is offline.
    }
  }

  /** Map saved settings to the shared engagement config and apply them app-wide (in place). */
  applySettings(settings: EngagementTypeSettings | null | undefined): void {
    const configs = (settings?.types ?? []).map<EngagementTypeConfig>(t => ({
      type: t.type,
      blurb: t.blurb,
      enabled: t.enabled,
      products: t.products,
      pricing: t.pricing,
      meetingNotes: t.meetingNotes,
      approvals: t.approvals,
      submitLabel: t.submitLabel,
      submitAction: t.submitAction,
      tagRequired: t.tagRequired,
      marketplaceRequired: t.marketplaceRequired
    }));
    applyEngagementConfigs(configs);
  }
}
