import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import {
  ApprovalConditionType,
  ApprovalRuleSetting,
  ApprovalRulesSettings
} from '@shared/data/approval-settings.model';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

@Component({
  selector: 'app-approval-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './approval-settings.component.html',
  styleUrl: './approval-settings.component.scss'
})
export class ApprovalSettingsComponent implements OnInit {
  private api = inject(ApiService);

  settings: ApprovalRulesSettings | null = null;
  loading = true;
  saving = false;
  error = '';
  success = '';

  /** Engagement types whose flow can include approvals — the ones a rule can target. */
  readonly engagementTypes = ['Private Offer', 'Free Trial', 'Hackathon', 'POC'];

  readonly conditionTypes: { value: ApprovalConditionType; label: string }[] = [
    { value: 'discountGreaterThan', label: 'Discount % exceeds threshold' },
    { value: 'durationMonthsGreaterThan', label: 'Contract duration (months) exceeds threshold' },
    { value: 'marketplacePresent', label: 'A marketplace is selected' },
    { value: 'always', label: 'Always required' }
  ];

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getApprovalRules().subscribe({
      next: s => { this.settings = s; this.loading = false; },
      error: e => { this.error = apiErrorMessage(e, 'Could not load approval rules.'); this.loading = false; }
    });
  }

  save() {
    if (!this.settings) return;
    this.error = '';
    this.success = '';
    this.saving = true;
    this.api.saveApprovalRules(this.settings).subscribe({
      next: s => {
        this.settings = s;
        this.saving = false;
        this.success = 'Approval rules saved. New and re-evaluated engagements will use them immediately.';
        setTimeout(() => this.success = '', 3500);
      },
      error: e => { this.saving = false; this.error = apiErrorMessage(e, 'Could not save approval rules.'); }
    });
  }

  reset() {
    this.error = '';
    this.api.resetApprovalRules().subscribe({
      next: s => {
        this.settings = s;
        this.success = 'Approval rules reset to defaults.';
        setTimeout(() => this.success = '', 3500);
      },
      error: e => { this.error = apiErrorMessage(e, 'Could not reset approval rules.'); }
    });
  }

  needsThreshold(rule: ApprovalRuleSetting): boolean {
    return rule.conditionType === 'discountGreaterThan' || rule.conditionType === 'durationMonthsGreaterThan';
  }

  thresholdLabel(rule: ApprovalRuleSetting): string {
    return rule.conditionType === 'discountGreaterThan' ? 'Discount % threshold' : 'Duration (months) threshold';
  }

  appliesTo(rule: ApprovalRuleSetting, type: string): boolean {
    return rule.engagementTypes.includes(type);
  }

  toggleType(rule: ApprovalRuleSetting, type: string) {
    const i = rule.engagementTypes.indexOf(type);
    if (i >= 0) rule.engagementTypes.splice(i, 1);
    else rule.engagementTypes.push(type);
  }
}
