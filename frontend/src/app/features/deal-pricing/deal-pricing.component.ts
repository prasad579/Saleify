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

  ngOnInit() {
    this.dealId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.dealId || this.dealId === 'new') {
      this.loading = false;
      this.error = 'Invalid deal. Create a deal first.';
      return;
    }
    this.loadDeal();
  }

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
          this.error = `Deal ${this.dealId} was not found.`;
          return;
        }
        this.selectedProducts = detail.selectedProducts || [];
        const listPrice = detail.suggestedPublicPricePerYear || this.deal.pricing?.publicPricePerYear || 0;
        this.pricing = mapBackendPricing(this.deal.pricing, listPrice);
        this.pricing.marketplaceFeePercent = detail.marketplaceFeePercent ?? this.pricing.marketplaceFeePercent;
        if (!this.deal.pricing && listPrice > 0) {
          this.pricing.publicPricePerYear = listPrice;
        }
        this.pricing = ensureYearlyDiscountRows(this.pricing);
        this.insight = detail.pricingInsight || '';
        this.loading = false;
        this.recalc();
      },
      error: (err) => {
        this.loading = false;
        this.error = apiErrorMessage(err, `Deal ${this.dealId} not found.`);
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
        void this.router.navigate(['/deals', this.dealId, 'meeting-notes']);
      },
      error: (err) => {
        this.saving = false;
        this.error = apiErrorMessage(err, 'Could not save pricing.');
      }
    });
  }
}
