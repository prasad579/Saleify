import { ScreenKey, applicableScreens } from '@shared/utils/engagement.util';

export interface DealDetailResponse {
  deal: any;
  selectedProducts?: any[];
  suggestedPublicPricePerYear?: number;
  marketplaceFeePercent?: number;
  pricingInsight?: string;
}

export function normalizeDealDetail(raw: any): DealDetailResponse {
  if (!raw) return { deal: null, selectedProducts: [] };
  if (raw.deal) {
    return {
      deal: raw.deal,
      selectedProducts: raw.selectedProducts ?? [],
      suggestedPublicPricePerYear: raw.suggestedPublicPricePerYear,
      marketplaceFeePercent: raw.marketplaceFeePercent,
      pricingInsight: raw.pricingInsight
    };
  }
  if (raw.id || raw.Id) {
    return { deal: raw, selectedProducts: raw.productIds ? [] : [] };
  }
  return { deal: null, selectedProducts: [] };
}

export function extractCreatedDealId(res: any): string | null {
  if (!res) return null;
  const deal = res.deal ?? res;
  return deal?.id ?? deal?.Id ?? null;
}

export function isCreateDealSuccess(res: any, id: string | null): boolean {
  if (!id) return false;
  return res?.success !== false;
}

export function dealContinuePath(deal: { id: string; stepNumber?: number; continueRoute?: string; engagementType?: string }) {
  if (deal.continueRoute) return `/deals/${deal.id}/${deal.continueRoute}`;
  // Map legacy step number to a screen, then snap to the first applicable screen for this engagement type.
  const legacy: ScreenKey[] = ['details', 'products', 'pricing', 'meeting-notes', 'approvals'];
  const step = Math.min(Math.max(deal.stepNumber ?? 1, 1), 5);
  const desired = legacy[step - 1];
  const screens = applicableScreens(deal.engagementType || 'Private Offer');
  // First applicable screen at or after the desired one (fall back to the last applicable).
  const target = screens.find(s => legacy.indexOf(s) >= legacy.indexOf(desired)) ?? screens[screens.length - 1] ?? 'details';
  return target === 'details' ? `/deals/${deal.id}/edit` : `/deals/${deal.id}/${target}`;
}

export function apiErrorMessage(err: any, fallback: string): string {
  if (err?.status === 0) {
    return 'Cannot reach the API. Start the backend: cd MarketplaceCopilot.Api && dotnet run (or run .\\start-demo.ps1), then click Retry.';
  }
  if (err?.status === 404) {
    return 'Approvals API not found — restart the backend to load the latest code, then click Retry.';
  }
  return err?.error?.message || fallback;
}
