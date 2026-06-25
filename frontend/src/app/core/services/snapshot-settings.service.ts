import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from '@core/services/api.service';
import { SnapshotSettings } from '@shared/data/snapshot.model';

/**
 * Loads the Snapshot & Email settings once and exposes them app-wide so the
 * "Generate Snapshot" / "Email Summary" buttons can be shown or hidden.
 * Defaults to enabled until the settings load, to avoid hiding buttons on a flash.
 */
@Injectable({ providedIn: 'root' })
export class SnapshotSettingsService {
  private api = inject(ApiService);
  readonly settings = signal<SnapshotSettings | null>(null);

  readonly snapshotButtonEnabled = computed(() => this.settings()?.snapshotButtonEnabled ?? true);
  readonly emailButtonEnabled = computed(() => this.settings()?.emailButtonEnabled ?? true);

  constructor() {
    this.refresh();
  }

  refresh() {
    this.api.getSnapshotSettings().subscribe({
      next: s => this.settings.set(s),
      error: () => { /* keep optimistic defaults */ }
    });
  }
}
