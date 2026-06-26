import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { HomeSettingsService } from '@core/services/home-settings.service';
import { ToastService } from '@core/services/toast.service';
import { HomeSettings } from '@shared/data/home-settings.model';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-home-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './home-settings.component.html',
  styleUrl: './home-settings.component.scss'
})
export class HomeSettingsComponent implements OnInit {
  private api = inject(ApiService);
  private homeSettings = inject(HomeSettingsService);
  private toast = inject(ToastService);

  settings: HomeSettings | null = null;
  loading = true;
  saving = false;

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getHomeSettings().subscribe({
      next: s => { this.settings = s; this.loading = false; },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not load home settings.')); this.loading = false; }
    });
  }

  save() {
    if (!this.settings) return;
    this.saving = true;
    this.api.saveHomeSettings(this.settings).subscribe({
      next: s => {
        this.settings = s;
        this.homeSettings.set(s); // reflect on the home page immediately
        this.saving = false;
        this.toast.success('Home dashboard saved.');
      },
      error: e => { this.saving = false; this.toast.error(apiErrorMessage(e, 'Could not save home settings.')); }
    });
  }

  reset() {
    this.api.resetHomeSettings().subscribe({
      next: s => {
        this.settings = s;
        this.homeSettings.set(s);
        this.toast.success('Home dashboard reset to defaults.');
      },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not reset home settings.')); }
    });
  }

  enabledCount(): number {
    return this.settings?.cards.filter(c => c.enabled).length ?? 0;
  }
}
