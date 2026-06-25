import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { SnapshotSettingsService } from '@core/services/snapshot-settings.service';
import { SnapshotSettings } from '@shared/data/snapshot.model';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-snapshot-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './snapshot-settings.component.html',
  styleUrl: './snapshot-settings.component.scss'
})
export class SnapshotSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private settingsService = inject(SnapshotSettingsService);

  settings: SnapshotSettings | null = null;
  loading = true;
  saving = false;
  error = '';
  success = '';

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getSnapshotSettings().subscribe({
      next: s => { this.settings = s; this.loading = false; },
      error: e => { this.error = apiErrorMessage(e, 'Could not load settings.'); this.loading = false; }
    });
  }

  save() {
    if (!this.settings) return;
    this.error = '';
    this.success = '';
    this.saving = true;
    this.api.saveSnapshotSettings(this.settings).subscribe({
      next: s => {
        this.settings = s;
        this.settingsService.refresh(); // update button visibility app-wide
        this.saving = false;
        this.success = 'Settings saved. Snapshots and emails will use them immediately.';
        setTimeout(() => this.success = '', 3500);
      },
      error: e => { this.saving = false; this.error = apiErrorMessage(e, 'Could not save settings.'); }
    });
  }

  reset() {
    this.error = '';
    this.api.resetSnapshotSettings().subscribe({
      next: s => {
        this.settings = s;
        this.settingsService.refresh();
        this.success = 'Settings reset to defaults.';
        setTimeout(() => this.success = '', 3500);
      },
      error: e => { this.error = apiErrorMessage(e, 'Could not reset settings.'); }
    });
  }
}
