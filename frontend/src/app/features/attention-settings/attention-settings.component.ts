import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { AttentionSettingsService } from '@core/services/attention-settings.service';
import { ToastService } from '@core/services/toast.service';
import { AttentionSettings } from '@shared/data/attention-settings.model';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-attention-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './attention-settings.component.html',
  styleUrl: './attention-settings.component.scss'
})
export class AttentionSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private svc = inject(AttentionSettingsService);
  private toast = inject(ToastService);

  settings: AttentionSettings | null = null;
  loading = true;
  saving = false;

  readonly windowOptions = [3, 5, 7, 14, 30];

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getAttentionSettings().subscribe({
      next: s => { this.settings = s; this.loading = false; },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not load alert settings.')); this.loading = false; }
    });
  }

  save() {
    if (!this.settings) return;
    this.saving = true;
    this.api.saveAttentionSettings(this.settings).subscribe({
      next: s => {
        this.settings = s;
        this.svc.set(s); // reflect on the home page immediately
        this.saving = false;
        this.toast.success('Alert settings saved.');
      },
      error: e => { this.saving = false; this.toast.error(apiErrorMessage(e, 'Could not save alert settings.')); }
    });
  }

  reset() {
    this.api.resetAttentionSettings().subscribe({
      next: s => { this.settings = s; this.svc.set(s); this.toast.success('Alert settings reset to defaults.'); },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not reset alert settings.')); }
    });
  }
}
