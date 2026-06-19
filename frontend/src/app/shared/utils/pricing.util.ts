export type DurationType = 'days' | 'months' | 'years';

export interface PricingFormState {
  offerType: string;
  contractStart: string;
  contractEnd: string;
  durationType: DurationType;
  durationValue: number;
  durationMonths: number;
  pricingMethod: string;
  publicPricePerYear: number;
  discountModel: 'same' | 'per-year';
  discountPercent: number;
  yearlyDiscountPercents: number[];
  absoluteContractPrice: number;
  proRateEnabled: boolean;
  flexiblePaymentsEnabled: boolean;
  installmentCount: number;
  marketplaceFeePercent: number;
}

export function parseIsoDate(value: string): Date | null {
  if (!value) return null;
  const d = new Date(value + 'T00:00:00');
  return Number.isNaN(d.getTime()) ? null : d;
}

export function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function addDuration(start: Date, value: number, type: DurationType): Date {
  const d = new Date(start);
  if (type === 'days') return new Date(d.setDate(d.getDate() + value - 1));
  if (type === 'months') {
    d.setMonth(d.getMonth() + value);
    d.setDate(d.getDate() - 1);
    return d;
  }
  d.setFullYear(d.getFullYear() + value);
  d.setDate(d.getDate() - 1);
  return d;
}

export function diffDays(start: Date, end: Date): number {
  const ms = end.getTime() - start.getTime();
  return Math.max(1, Math.round(ms / 86400000) + 1);
}

export function durationLabel(type: DurationType): string {
  return type === 'days' ? 'days' : type === 'months' ? 'months' : 'years';
}

export function canUsePerYearDiscount(type: DurationType): boolean {
  return type === 'years';
}

export function defaultPricingState(publicPrice = 0): PricingFormState {
  const start = new Date();
  const end = addDuration(start, 3, 'years');
  return {
    offerType: 'Direct Private Offer',
    contractStart: toIsoDate(start),
    contractEnd: toIsoDate(end),
    durationType: 'years',
    durationValue: 3,
    durationMonths: 36,
    pricingMethod: 'Discount Based',
    publicPricePerYear: publicPrice,
    discountModel: 'same',
    discountPercent: 15,
    yearlyDiscountPercents: [15, 15, 15],
    absoluteContractPrice: 0,
    proRateEnabled: false,
    flexiblePaymentsEnabled: false,
    installmentCount: 4,
    marketplaceFeePercent: 5
  };
}

export function mapBackendPricing(p: any, publicPrice: number): PricingFormState {
  const base = defaultPricingState(publicPrice || p?.publicPricePerYear || 0);
  if (!p) return base;

  const durationType = (p.durationType || (p.durationMonths % 12 === 0 ? 'years' : 'months')) as DurationType;
  const durationValue = p.durationValue || (durationType === 'years'
    ? Math.max(1, Math.round((p.durationMonths || 36) / 12))
    : (p.durationMonths || 36));

  return {
    ...base,
    offerType: p.offerType || base.offerType,
    contractStart: p.contractStart?.includes('-') ? p.contractStart.slice(0, 10) : base.contractStart,
    contractEnd: p.contractEnd?.includes('-') ? p.contractEnd.slice(0, 10) : base.contractEnd,
    durationType,
    durationValue,
    durationMonths: p.durationMonths || base.durationMonths,
    pricingMethod: p.pricingMethod || base.pricingMethod,
    publicPricePerYear: p.publicPricePerYear || publicPrice,
    discountModel: p.discountModel?.toLowerCase().includes('different') ? 'per-year' : 'same',
    discountPercent: p.discountPercent ?? base.discountPercent,
    yearlyDiscountPercents: p.yearlyDiscountPercents?.length
      ? [...p.yearlyDiscountPercents]
      : (p.yearlyBreakdown || []).map((r: any) => r.discountPercent ?? p.discountPercent ?? 15),
    absoluteContractPrice: p.absoluteContractPrice || p.netPriceBeforeFees || 0,
    proRateEnabled: !!p.proRateEnabled,
    flexiblePaymentsEnabled: !!p.flexiblePaymentsEnabled,
    installmentCount: p.installmentCount || 4,
    marketplaceFeePercent: p.marketplaceFeePercent ?? base.marketplaceFeePercent
  };
}

export function buildPricingPayload(form: PricingFormState) {
  return {
    offerType: form.offerType,
    contractStart: form.contractStart,
    contractEnd: form.contractEnd,
    durationType: form.durationType,
    durationValue: form.durationValue,
    durationMonths: form.durationMonths,
    pricingMethod: form.pricingMethod,
    publicPricePerYear: form.publicPricePerYear,
    discountModel: form.discountModel === 'per-year'
      ? 'Different discount per year'
      : 'Same discount for entire contract',
    discountPercent: form.discountPercent,
    yearlyDiscountPercents: form.discountModel === 'per-year'
      ? form.yearlyDiscountPercents.slice(0, form.durationValue)
      : [],
    absoluteContractPrice: form.absoluteContractPrice,
    proRateEnabled: form.proRateEnabled,
    flexiblePaymentsEnabled: form.flexiblePaymentsEnabled,
    installmentCount: form.installmentCount,
    marketplaceFeePercent: form.marketplaceFeePercent
  };
}

export function syncEndFromDuration(form: PricingFormState): PricingFormState {
  const start = parseIsoDate(form.contractStart);
  if (!start || form.durationValue <= 0) return form;
  return { ...form, contractEnd: toIsoDate(addDuration(start, form.durationValue, form.durationType)) };
}

export function syncDurationFromDates(form: PricingFormState): PricingFormState {
  const start = parseIsoDate(form.contractStart);
  const end = parseIsoDate(form.contractEnd);
  if (!start || !end || end < start) return form;
  const days = diffDays(start, end);
  if (form.durationType === 'days') return { ...form, durationValue: days };
  if (form.durationType === 'months') return { ...form, durationValue: Math.max(1, Math.round(days / 30.4375)) };
  return { ...form, durationValue: Math.max(1, Math.round(days / 365.25)) };
}

export function ensureYearlyDiscountRows(form: PricingFormState): PricingFormState {
  if (form.discountModel !== 'per-year' || form.durationType !== 'years') return form;
  const rows = [...form.yearlyDiscountPercents];
  while (rows.length < form.durationValue) rows.push(form.discountPercent);
  return { ...form, yearlyDiscountPercents: rows.slice(0, form.durationValue) };
}
