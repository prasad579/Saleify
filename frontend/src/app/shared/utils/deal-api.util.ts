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

export function dealContinuePath(deal: { id: string; stepNumber?: number; continueRoute?: string }) {
  if (deal.continueRoute) return `/deals/${deal.id}/${deal.continueRoute}`;
  const step = deal.stepNumber ?? 1;
  if (step >= 5) return `/deals/${deal.id}/approvals`;
  if (step >= 4) return `/deals/${deal.id}/meeting-notes`;
  if (step >= 3) return `/deals/${deal.id}/meeting-notes`;
  if (step >= 2) return `/deals/${deal.id}/pricing`;
  return `/deals/${deal.id}/products`;
}

export function apiErrorMessage(err: any, fallback: string): string {
  if (err?.status === 0) {
    return 'Cannot reach the API. Start the backend: cd backend && dotnet run (or run .\\start-demo.ps1), then click Retry.';
  }
  if (err?.status === 404) {
    return 'Approvals API not found — restart the backend to load the latest code, then click Retry.';
  }
  return err?.error?.message || fallback;
}
