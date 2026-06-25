import { Component, Input } from '@angular/core';
import { ScreenKey, stepperSteps } from '@shared/utils/engagement.util';

@Component({
  selector: 'app-deal-stepper',
  standalone: true,
  template: `
    <div class="stepper">
      @for (s of steps; track s.key; let i = $index) {
        <span class="step"
          [class.done]="activeIndex > i"
          [class.active]="activeIndex === i">{{ i + 1 }}. {{ s.label }}</span>
      }
    </div>
  `
})
export class DealStepperComponent {
  /** Engagement type — determines which steps are shown. */
  @Input() engagementType = 'Private Offer';
  /** The current screen key (preferred). */
  @Input() activeKey: ScreenKey | null = null;
  /**
   * Legacy numeric step (1-5) for callers not yet migrated.
   * Maps to the old fixed order: 1 details, 2 products, 3 pricing, 4 meeting-notes, 5 approvals.
   */
  @Input() activeStep = 1;

  private readonly legacyOrder: ScreenKey[] = ['details', 'products', 'pricing', 'meeting-notes', 'approvals'];

  get steps() {
    return stepperSteps(this.engagementType);
  }

  get activeIndex(): number {
    const key = this.activeKey ?? this.legacyOrder[Math.min(Math.max(this.activeStep, 1), 5) - 1];
    const idx = this.steps.findIndex(s => s.key === key);
    return idx >= 0 ? idx : 0;
  }
}
