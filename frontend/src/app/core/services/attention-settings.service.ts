import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import { AttentionSettings } from '@shared/data/attention-settings.model';

const DEFAULTS: AttentionSettings = {
  alertEnabled: true,
  upcomingEnabled: true,
  upcomingWindowDays: 7,
  includeTasks: true,
  includeReminders: true,
  includeEngagements: true
};

/**
 * Loads the home alert / upcoming settings and exposes them app-wide. Defaults to "on" until
 * loaded so the alert isn't hidden on a flash. Call refresh() after saving to reflect changes.
 */
@Injectable({ providedIn: 'root' })
export class AttentionSettingsService {
  private api = inject(ApiService);
  readonly settings = signal<AttentionSettings>({ ...DEFAULTS });

  constructor() { this.refresh(); }

  refresh() {
    this.api.getAttentionSettings().subscribe({
      next: s => this.settings.set(s ?? { ...DEFAULTS }),
      error: () => { /* keep optimistic defaults */ }
    });
  }

  set(s: AttentionSettings) { this.settings.set(s); }
}
