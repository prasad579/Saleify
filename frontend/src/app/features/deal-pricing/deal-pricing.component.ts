import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ApiService } from '@core/services/api.service';
import { apiErrorMessage, normalizeDealDetail } from '@shared/utils/deal-api.util';
import { DealStepperComponent } from '@shared/components/deal-stepper/deal-stepper.component';
import { DealFlowFooterComponent } from '@shared/components/deal-flow-footer/deal-flow-footer.component';
import {
  PricingFormState,
  buildPricingPayload,
  canUsePerYearDiscount,
  defaultPricingState,
  durationLabel,
  ensureYearlyDiscountRows,
  mapBackendPricing,
  syncDurationFromDates,
  syncEndFromDuration
} from '@shared/utils/pricing.util';
import { TRIAL_DAY_OPTIONS, isNoMoneyOffer, isContractOffer, offerTypesForEngagement } from '@shared/data/lookups';
import { nextScreenPath, prevScreenPath, screenApplies } from '@shared/utils/engagement.util';

@Component({
  selector: 'app-deal-pricing',
  standalone: true,
  imports: [FormsModule, DecimalPipe, RouterLink, DealStepperComponent, DealFlowFooterComponent],
  templateUrl: './deal-pricing.component.html',
  styleUrl: './deal-pricing.component.scss'
})
export class DealPricingComponent implements OnInit {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  dealId = '';
  deal: any = null;
  selectedProducts: any[] = [];
  pricing: PricingFormState = defaultPricingState();
  preview: any = null;
  insight = '';
  loading = true;
  saving = false;
  error = '';
  private syncing = false;

  durationTypes: { value: PricingFormState['durationType']; label: string }[] = [
    { value: 'days', label: 'Days' },
    { value: 'months', label: 'Months' },
    { value: 'years', label: 'Years' }
  ];

  trialDayOptions = TRIAL_DAY_OPTIONS;

  /** Offer types relevant to this engagement type (POC shows the pilot option, etc.). */
  get offerTypes(): string[] {
    return offerTypesForEngagement(this.engagementType);
  }

  offerTypeBlurb(type: string): string {
    switch (type) {
      case 'Free Trial': return 'Time-boxed trial — no charge until consumption limit';
      case 'POC / Pilot': return 'Time-boxed proof of concept — no charge during the pilot';
      case 'Direct Private Offer': return 'Offer published directly to customer';
      case 'Reseller Private Offer': return 'Offer via reseller partner';
      case 'Renewal': return 'Renew an existing contract';
      default: return '';
    }
  }

  /** No-money, time-boxed offer (free trial or POC / pilot). */
  get isNoMoney(): boolean {
    return isNoMoneyOffer(this.pricing.offerType);
  }

  get isPoc(): boolean {
    return /poc|pilot/i.test(this.pricing.offerType);
  }

  // Labels for the time-boxed section, which serves both free trials and POC / pilots.
  get evalTitle(): string { return this.isPoc ? 'POC / pilot period' : 'Free trial period'; }
  get evalLengthLabel(): string { return this.isPoc ? 'POC / pilot length' : 'Trial length'; }
  get evalStartLabel(): string { return this.isPoc ? 'POC start date' : 'Trial start date'; }
  get evalEndLabel(): string { return this.isPoc ? 'POC end date (auto)' : 'Trial end date (auto)'; }
  get evalBanner(): string {
    return this.isPoc
      ? 'ℹ️ A POC / pilot is a time-boxed evaluation and carries no contract value. Convert it to a private offer once it succeeds.'
      : 'ℹ️ Free trials carry no contract value. On crossing the included consumption limit, the customer is charged at standard list rates per the EULA.';
  }

  /** Direct/Reseller private offer or renewal — show the full contract & pricing section. */
  get showContractSection(): boolean {
    return isContractOffer(this.pricing.offerType);
  }

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.dealId || this.dealId === 'new') {
      this.loading = false;
      this.error = 'Invalid engagement. Create an engagement first.';
      return;
    }
    this.loadDeal();
  }

  get engagementType(): string { return this.deal?.engagementType || 'Private Offer'; }
  get backPath(): string { return prevScreenPath(this.engagementType, this.dealId, 'pricing') || `/deals/${this.dealId}/products`; }

  get showPerYearDiscount(): boolean {
    return this.pricing.pricingMethod === 'Discount Based' && canUsePerYearDiscount(this.pricing.durationType);
  }

  get isAbsolutePrice(): boolean {
    return this.pricing.pricingMethod === 'Absolute Price';
  }

  get yearlyRows(): any[] {
    return this.preview?.yearlyBreakdown || [];
  }

  get installments(): any[] {
    return this.preview?.installmentSchedule || [];
  }

  loadDeal() {
    this.loading = true;
    this.error = '';
    this.api.getDeal(this.dealId).subscribe({
      next: (raw: any) => {
        const detail = normalizeDealDetail(raw);
        this.deal = detail.deal;
        if (!this.deal) {
          this.loading = false;
          this.error = `Engagement ${this.dealId} was not found.`;
          return;
        }
        // Skip this screen if Pricing doesn't apply to the engagement type.
        if (!screenApplies(this.engagementType, 'pricing')) {
          const next = nextScreenPath(this.engagementType, this.dealId, 'pricing') || `/deals/${this.dealId}`;
          this.router.navigateByUrl(next);
          return;
        }
        this.selectedProducts = detail.selectedProducts || [];
        const listPrice = detail.suggestedPublicPricePerYear || this.deal.pricing?.publicPricePerYear || 0;
        this.pricing = mapBackendPricing(this.deal.pricing, listPrice);
        this.pricing.marketplaceFeePercent = detail.marketplaceFeePercent ?? this.pricing.marketplaceFeePercent;
        if (!this.deal.pricing && listPrice > 0) {
          this.pricing.publicPricePerYear = listPrice;
        }
        // Until pricing is saved (step 3), default to the engagement's preferred offer type
        // (e.g. POC → "POC / Pilot"); afterwards keep the saved choice if it's still valid.
        const pricingSaved = (this.deal.stepNumber || 0) >= 3 && !!this.deal.pricing;
        if (!pricingSaved || !this.offerTypes.includes(this.pricing.offerType)) {
          this.pricing.offerType = this.offerTypes[0];
        }
        if (this.isNoMoney && (!this.pricing.trialDays || this.pricing.trialDays <= 0)) {
          this.pricing.trialDays = 14;
        }
        this.pricing = ensureYearlyDiscountRows(this.pricing);
        this.insight = detail.pricingInsight || '';
        this.loading = false;
        this.recalc();
      },
      error: (err) => {
        this.loading = false;
        this.error = apiErrorMessage(err, `Engagement ${this.dealId} not found.`);
      }
    });
  }

  onDurationTypeChange() {
    if (!canUsePerYearDiscount(this.pricing.durationType) && this.pricing.discountModel === 'per-year') {
      this.pricing.discountModel = 'same';
    }
    this.onDurationValueChange();
  }

  onStartDateChange() {
    if (this.syncing) return;
    this.syncing = true;
    this.pricing = syncEndFromDuration(this.pricing);
    this.syncing = false;
    this.recalc();
  }

  onEndDateChange() {
    if (this.syncing) return;
    this.syncing = true;
    this.pricing = syncDurationFromDates(this.pricing);
    if (!canUsePerYearDiscount(this.pricing.durationType) && this.pricing.discountModel === 'per-year') {
      this.pricing.discountModel = 'same';
    }
    this.pricing = ensureYearlyDiscountRows(this.pricing);
    this.syncing = false;
    this.recalc();
  }

  onDurationValueChange() {
    if (this.syncing) return;
    this.syncing = true;
    this.pricing = syncEndFromDuration(this.pricing);
    this.pricing = ensureYearlyDiscountRows(this.pricing);
    this.syncing = false;
    this.recalc();
  }

  setOfferType(type: string) {
    this.pricing.offerType = type;
    if (this.isNoMoney) {
      // Free trial / POC / pilot is time-boxed and carries no money — seed sensible defaults.
      this.pricing.contractStart = this.pricing.contractStart || new Date().toISOString().slice(0, 10);
      if (!this.pricing.trialDays || this.pricing.trialDays <= 0) this.pricing.trialDays = 14;
    }
    this.recalc();
  }

  setTrialDays(days: number) {
    this.pricing.trialDays = days;
    this.recalc();
  }

  onTrialDaysChange() {
    if (this.pricing.trialDays < 1) this.pricing.trialDays = 1;
    this.recalc();
  }

  setPricingMethod(method: string) {
    this.pricing.pricingMethod = method;
    if (method === 'Absolute Price' && !this.pricing.absoluteContractPrice) {
      this.pricing.absoluteContractPrice = this.preview?.netPriceBeforeFees || this.pricing.publicPricePerYear;
    }
    this.recalc();
  }

  setDiscountModel(model: 'same' | 'per-year') {
    if (model === 'per-year' && !this.showPerYearDiscount) return;
    this.pricing.discountModel = model;
    this.pricing = ensureYearlyDiscountRows(this.pricing);
    this.recalc();
  }

  updateYearDiscount(index: number, value: number) {
    this.pricing.yearlyDiscountPercents[index] = value;
    this.recalc();
  }

  durationUnitLabel(): string {
    return durationLabel(this.pricing.durationType);
  }

  recalc() {
    if (!this.dealId) return;
    const payload = buildPricingPayload(this.pricing);
    this.api.previewPricing(this.dealId, payload).subscribe({
      next: (res: any) => {
        this.preview = res.pricing;
        this.insight = res.insight || this.insight;
        this.selectedProducts = res.selectedProducts || this.selectedProducts;
        if (this.preview?.contractEnd) this.pricing.contractEnd = this.preview.contractEnd.slice(0, 10);
        if (this.preview?.contractStart) this.pricing.contractStart = this.preview.contractStart.slice(0, 10);
        if (this.preview?.durationValue) this.pricing.durationValue = this.preview.durationValue;
        if (this.preview?.durationType) this.pricing.durationType = this.preview.durationType;
        if (this.isAbsolutePrice && !this.pricing.absoluteContractPrice && this.preview?.netPriceBeforeFees) {
          this.pricing.absoluteContractPrice = this.preview.netPriceBeforeFees;
        }
        this.error = '';
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not calculate pricing.');
      }
    });
  }

  save() {
    if (!this.preview) {
      this.error = 'Pricing has not been calculated yet.';
      return;
    }
    this.saving = true;
    this.error = '';
    const payload = { ...buildPricingPayload(this.pricing), ...this.preview };
    this.api.setPricing(this.dealId, payload).subscribe({
      next: () => {
        this.saving = false;
        const next = nextScreenPath(this.engagementType, this.dealId, 'pricing') || `/deals/${this.dealId}/meeting-notes`;
        void this.router.navigateByUrl(next);
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not save pricing.');
      }
    });
  }
}
