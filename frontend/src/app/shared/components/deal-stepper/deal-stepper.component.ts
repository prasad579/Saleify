import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-deal-stepper',
  standalone: true,
  template: `
    <div class="stepper">
      <span class="step" [class.done]="activeStep > 1" [class.active]="activeStep === 1">1. Deal Information</span>
      <span class="step" [class.done]="activeStep > 2" [class.active]="activeStep === 2">2. Select Products</span>
      <span class="step" [class.done]="activeStep > 3" [class.active]="activeStep === 3">3. Configure Pricing</span>
      <span class="step" [class.done]="activeStep > 4" [class.active]="activeStep === 4">4. Meeting Notes</span>
      <span class="step" [class.done]="activeStep > 5" [class.active]="activeStep === 5">5. Approvals</span>
    </div>
  `
})
export class DealStepperComponent {
  @Input() activeStep = 1;
}
