import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import { HomeSettings } from '@shared/data/home-settings.model';

/**
 * Loads the home / dashboard layout and exposes which cards are enabled, so the home page can
 * show/hide each section. Defaults to "enabled" until the settings load to avoid a flash of
 * hidden cards. Call refresh() after saving to reflect changes without a reload.
 */
@Injectable({ providedIn: 'root' })
export class HomeSettingsService {
  private api = inject(ApiService);
  readonly settings = signal<HomeSettings | null>(null);

  constructor() {
    this.refresh();
  }

  refresh() {
    this.api.getHomeSettings().subscribe({
      next: s => this.settings.set(s),
      error: () => { /* keep optimistic defaults */ }
    });
  }

  set(settings: HomeSettings) {
    this.settings.set(settings);
  }

  /** Whether a card is shown. Unknown keys (and the pre-load state) default to visible. */
  enabled(key: string): boolean {
    const cards = this.settings()?.cards;
    if (!cards) return true;
    const card = cards.find(c => c.key === key);
    return card ? card.enabled : true;
  }
}
