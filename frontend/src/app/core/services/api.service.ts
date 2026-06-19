import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';

export { dealContinuePath } from '@shared/utils/deal-api.util';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getDashboard() { return this.http.get(`${this.base}/dashboard`); }
  getDeals() { return this.http.get(`${this.base}/deals`); }
  getDealStats() { return this.http.get(`${this.base}/deals/stats`); }
  getDeal(id: string) { return this.http.get(`${this.base}/deals/${id}`); }
  getLookups() { return this.http.get(`${this.base}/lookups`); }
  createDeal(deal: unknown) { return this.http.post(`${this.base}/deals`, deal); }
  updateDeal(id: string, deal: unknown) { return this.http.put(`${this.base}/deals/${id}`, deal); }
  setProducts(id: string, productIds: string[]) { return this.http.post(`${this.base}/deals/${id}/products`, productIds); }
  previewPricing(id: string, pricing: unknown) { return this.http.post(`${this.base}/deals/${id}/pricing/preview`, pricing); }
  setPricing(id: string, pricing: unknown) { return this.http.post(`${this.base}/deals/${id}/pricing`, pricing); }
  setMeetingNotes(id: string, notes: unknown) { return this.http.post(`${this.base}/deals/${id}/meeting-notes`, notes); }
  getApprovals(id: string) { return this.http.get(`${this.base}/deals/${id}/approvals`); }
  enterApprovals(id: string) { return this.http.post(`${this.base}/deals/${id}/approvals/enter`, {}); }
  approvalAction(id: string, body: unknown) { return this.http.post(`${this.base}/deals/${id}/approvals/action`, body); }
  regenerateApprovalDocuments(id: string) { return this.http.post(`${this.base}/deals/${id}/approvals/regenerate-documents`, {}); }
  submitApprovals(id: string) { return this.http.post(`${this.base}/deals/${id}/approvals/submit`, {}); }
  unlockApprovals(id: string) { return this.http.post(`${this.base}/deals/${id}/approvals/unlock`, {}); }
  documentViewUrl(dealId: string, docId: string) {
    return `${this.base}/deals/${dealId}/approvals/documents/${docId}`;
  }
  documentDownloadUrl(dealId: string, docId: string) {
    return `${this.base}/deals/${dealId}/approvals/documents/${docId}/download`;
  }
  getDealHistory(id: string) { return this.http.get(`${this.base}/deals/${id}/history`); }

  getProducts(params?: { marketplace?: string; family?: string; search?: string }) {
    const q = new URLSearchParams();
    if (params?.marketplace) q.set('marketplace', params.marketplace);
    if (params?.family) q.set('family', params.family);
    if (params?.search) q.set('search', params.search);
    const query = q.toString();
    return this.http.get(`${this.base}/products${query ? '?' + query : ''}`);
  }

  login(body: { email: string; password: string }) { return this.http.post(`${this.base}/auth/login`, body); }
  signup(body: { email: string; password: string; fullName: string }) { return this.http.post(`${this.base}/auth/signup`, body); }
  verifyEmail(email: string) { return this.http.post(`${this.base}/auth/verify-email`, { email }); }
  approveRole(email: string, role?: string) { return this.http.post(`${this.base}/auth/approve-role`, { email, fullName: role }); }
  getAuthProviders() { return this.http.get(`${this.base}/auth/providers`); }
  extractInsights(notes: string, dealId?: string) { return this.http.post(`${this.base}/ai/extract-insights`, { notes, dealId }); }
  copilotChat(message: string, dealId?: string) { return this.http.post<{ reply: string }>(`${this.base}/ai/chat`, { message, dealId }); }
}
