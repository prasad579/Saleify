import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import {
  DashboardInsights,
  EmailSummaryRequest,
  EmailSummaryResponse,
  EngagementSnapshot,
  SnapshotRequest,
  SnapshotSettings
} from '@shared/data/snapshot.model';
import { Person } from '@shared/data/lookups';
import { ApprovalRulesSettings } from '@shared/data/approval-settings.model';
import { EngagementTypeSettings } from '@shared/data/engagement-types.model';
import { HomeSettings } from '@shared/data/home-settings.model';
import { AuditLogPage } from '@shared/data/audit.model';
import { OfferRequest, CaptureResponseRequest } from '@shared/data/offer-request.model';

export { dealContinuePath } from '@shared/utils/deal-api.util';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getDashboard() { return this.http.get(`${this.base}/dashboard`); }
  getDashboardInsights() { return this.http.get<DashboardInsights>(`${this.base}/dashboard/insights`); }

  // Engagement Snapshot (executive summary)
  generateSnapshot(request: SnapshotRequest) { return this.http.post<EngagementSnapshot>(`${this.base}/snapshot`, request); }
  emailSnapshot(request: EmailSummaryRequest) { return this.http.post<EmailSummaryResponse>(`${this.base}/snapshot/email`, request); }
  getSnapshotSettings() { return this.http.get<SnapshotSettings>(`${this.base}/snapshot/settings`); }
  saveSnapshotSettings(settings: SnapshotSettings) { return this.http.put<SnapshotSettings>(`${this.base}/snapshot/settings`, settings); }
  resetSnapshotSettings() { return this.http.post<SnapshotSettings>(`${this.base}/snapshot/settings/reset`, {}); }

  // Approval rules settings (which reviews an engagement requires)
  getApprovalRules() { return this.http.get<ApprovalRulesSettings>(`${this.base}/approval-rules`); }
  saveApprovalRules(settings: ApprovalRulesSettings) { return this.http.put<ApprovalRulesSettings>(`${this.base}/approval-rules`, settings); }
  resetApprovalRules() { return this.http.post<ApprovalRulesSettings>(`${this.base}/approval-rules/reset`, {}); }
  getDeals(view?: 'active' | 'archived' | 'all') {
    const q = view && view !== 'active' ? `?view=${view}` : '';
    return this.http.get(`${this.base}/deals${q}`);
  }
  unlockEngagementEdits(id: string) { return this.http.post(`${this.base}/deals/${id}/unlock-edits`, {}); }
  archiveDeal(id: string) { return this.http.post(`${this.base}/deals/${id}/archive`, {}); }
  unarchiveDeal(id: string) { return this.http.post(`${this.base}/deals/${id}/unarchive`, {}); }
  deleteDeal(id: string) { return this.http.delete(`${this.base}/deals/${id}`); }
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
  submitEngagement(id: string) { return this.http.post(`${this.base}/deals/${id}/submit-engagement`, {}); }
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

  // Campaign / event tags
  getCampaignEvents() { return this.http.get(`${this.base}/campaign-events`); }
  getCampaignEvent(id: string) { return this.http.get(`${this.base}/campaign-events/${id}`); }
  createCampaignEvent(ev: unknown) { return this.http.post(`${this.base}/campaign-events`, ev); }
  updateCampaignEvent(id: string, ev: unknown) { return this.http.put(`${this.base}/campaign-events/${id}`, ev); }
  deleteCampaignEvent(id: string) { return this.http.delete(`${this.base}/campaign-events/${id}`); }
  toggleCampaignEventPause(id: string) { return this.http.post(`${this.base}/campaign-events/${id}/toggle-pause`, {}); }
  getCampaignConversion(id: string) { return this.http.get(`${this.base}/campaign-events/${id}/conversion`); }

  // People (engagement owners) — customizable list managed from Settings → People
  getPeople() { return this.http.get<Person[]>(`${this.base}/people`); }
  savePerson(person: Partial<Person>) { return this.http.put<Person>(`${this.base}/people`, person); }
  togglePerson(id: string) { return this.http.post<Person>(`${this.base}/people/${id}/toggle`, {}); }
  deletePerson(id: string) { return this.http.delete(`${this.base}/people/${id}`); }
  resetPeople() { return this.http.post<Person[]>(`${this.base}/people/reset`, {}); }

  // Engagement types (configurable catalog + per-type applicable sections)
  getEngagementTypes() { return this.http.get<EngagementTypeSettings>(`${this.base}/engagement-types`); }
  saveEngagementTypes(settings: EngagementTypeSettings) { return this.http.put<EngagementTypeSettings>(`${this.base}/engagement-types`, settings); }
  resetEngagementTypes() { return this.http.post<EngagementTypeSettings>(`${this.base}/engagement-types/reset`, {}); }

  // Home / dashboard layout (which cards appear on the home page)
  getHomeSettings() { return this.http.get<HomeSettings>(`${this.base}/home-settings`); }
  saveHomeSettings(settings: HomeSettings) { return this.http.put<HomeSettings>(`${this.base}/home-settings`, settings); }
  resetHomeSettings() { return this.http.post<HomeSettings>(`${this.base}/home-settings/reset`, {}); }

  // Offer requests (engagements pushed to a destination + responses)
  getOfferRequests() { return this.http.get<OfferRequest[]>(`${this.base}/offer-requests`); }
  getOfferRequest(id: string) { return this.http.get<OfferRequest>(`${this.base}/offer-requests/${id}`); }
  captureOfferResponse(id: string, body: CaptureResponseRequest) {
    return this.http.post<OfferRequest>(`${this.base}/offer-requests/${id}/response`, body);
  }

  // Global audit log (who changed what, when, across the app)
  getAuditLog(params?: { category?: string; entityId?: string; search?: string; page?: number; pageSize?: number }) {
    const q = new URLSearchParams();
    if (params?.category) q.set('category', params.category);
    if (params?.entityId) q.set('entityId', params.entityId);
    if (params?.search) q.set('search', params.search);
    if (params?.page) q.set('page', String(params.page));
    if (params?.pageSize) q.set('pageSize', String(params.pageSize));
    const query = q.toString();
    return this.http.get<AuditLogPage>(`${this.base}/audit${query ? '?' + query : ''}`);
  }

  // Engagement playbooks (configurable "what's next" guidance)
  getPlaybooks() { return this.http.get(`${this.base}/engagement-playbooks`); }
  savePlaybook(playbook: unknown) { return this.http.put(`${this.base}/engagement-playbooks`, playbook); }
  resetPlaybooks() { return this.http.post(`${this.base}/engagement-playbooks/reset`, {}); }

  login(body: { email: string; password: string }) { return this.http.post(`${this.base}/auth/login`, body); }
  signup(body: { email: string; password: string; fullName: string }) { return this.http.post(`${this.base}/auth/signup`, body); }
  verifyEmail(email: string) { return this.http.post(`${this.base}/auth/verify-email`, { email }); }
  approveRole(email: string, role?: string) { return this.http.post(`${this.base}/auth/approve-role`, { email, fullName: role }); }
  getAuthProviders() { return this.http.get(`${this.base}/auth/providers`); }
  extractInsights(notes: string, dealId?: string) { return this.http.post(`${this.base}/ai/extract-insights`, { notes, dealId }); }
  copilotChat(message: string, dealId?: string) { return this.http.post<{ reply: string }>(`${this.base}/ai/chat`, { message, dealId }); }
}
