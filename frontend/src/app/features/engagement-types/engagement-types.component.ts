import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { EngagementConfigService } from '@core/services/engagement-config.service';
import { ToastService } from '@core/services/toast.service';
import { EngagementTypeSetting, EngagementTypeSettings } from '@shared/data/engagement-types.model';
import { Visibility } from '@shared/utils/engagement.util';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-engagement-types',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './engagement-types.component.html',
  styleUrl: './engagement-types.component.scss'
})
export class EngagementTypesComponent implements OnInit {
  private api = inject(ApiService);
  private engagementCfg = inject(EngagementConfigService);
  private toast = inject(ToastService);

  settings: EngagementTypeSettings | null = null;
  loading = true;
  saving = false;
  error = '';

  /** Sections whose applicability is configurable per engagement type. */
  readonly sections: { key: 'products' | 'pricing' | 'meetingNotes' | 'approvals'; label: string }[] = [
    { key: 'products', label: 'Products' },
    { key: 'pricing', label: 'Pricing' },
    { key: 'meetingNotes', label: 'Meeting Notes' },
    { key: 'approvals', label: 'Approvals' }
  ];

  readonly visibilities: { value: Visibility; label: string }[] = [
    { value: 'yes', label: 'Required' },
    { value: 'optional', label: 'Optional' },
    { value: 'no', label: 'Not applicable' }
  ];

  readonly submitActions = [
    { value: 'submit', label: 'Submit to SaaSify' },
    { value: 'complete', label: 'Mark completed' },
    { value: 'convert-later', label: 'Save & convert later' }
  ];

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getEngagementTypes().subscribe({
      next: s => { this.settings = s; this.loading = false; },
      error: e => { this.error = apiErrorMessage(e, 'Could not load engagement types.'); this.loading = false; }
    });
  }

  save() {
    if (!this.settings) return;
    this.error = '';
    this.saving = true;
    this.api.saveEngagementTypes(this.settings).subscribe({
      next: s => {
        this.settings = s;
        this.engagementCfg.applySettings(s); // reflect changes app-wide immediately
        this.saving = false;
        this.toast.success('Engagement types saved. Enabled types now apply across the app.');
      },
      error: e => { this.saving = false; this.toast.error(apiErrorMessage(e, 'Could not save engagement types.')); }
    });
  }

  reset() {
    this.error = '';
    this.api.resetEngagementTypes().subscribe({
      next: s => {
        this.settings = s;
        this.engagementCfg.applySettings(s);
        this.toast.success('Engagement types reset to defaults.');
      },
      error: e => { this.toast.error(apiErrorMessage(e, 'Could not reset engagement types.')); }
    });
  }

  sectionValue(t: EngagementTypeSetting, key: 'products' | 'pricing' | 'meetingNotes' | 'approvals'): Visibility {
    return t[key];
  }

  setSectionValue(t: EngagementTypeSetting, key: 'products' | 'pricing' | 'meetingNotes' | 'approvals', value: Visibility) {
    t[key] = value;
  }

  enabledCount(): number {
    return this.settings?.types.filter(t => t.enabled).length ?? 0;
  }
}
