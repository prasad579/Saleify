/**
 * Engagement Snapshot — executive summary types shared between the snapshot
 * component, the launcher service, and the API service. Mirrors the backend
 * EngagementSnapshot / SnapshotRequest contracts.
 */

export type SnapshotScope = 'engagement' | 'event' | 'filtered' | 'dashboard';

export interface SnapshotRequest {
  scope: SnapshotScope;
  dealId?: string;
  eventId?: string;
  owner?: string;
  stage?: string;
  status?: string;
  tag?: string;
  marketplace?: string;
  engagementType?: string;
  search?: string;
  openOnly?: boolean;
}

export interface EmailSummaryRequest extends SnapshotRequest {
  to: string[];
  cc?: string[];
  subject?: string;
}

export interface SnapshotCount {
  label: string;
  count: number;
}

export interface EventInfoSection {
  name: string;
  startDate: string;
  endDate: string;
  status: string;
}

export interface EngagementSummarySection {
  total: number;
  byType: SnapshotCount[];
}

export interface PipelineSummarySection {
  expectedPipelineValue: number;
  expectedPipelineDisplay: string;
  activePrivateOffers: number;
}

export interface AttentionRow {
  customer: string;
  engagementType: string;
  owner: string;
  status: string;
  nextActionDate: string;
  dealId: string;
  link: string;
}

export interface PrivateOfferRow {
  customer: string;
  marketplace: string;
  offerValue: string;
  status: string;
  expectedCloseDate: string;
  dealId: string;
  link: string;
}

export interface SnapshotFieldSetting {
  key: string;
  label: string;
  enabled: boolean;
}

export interface SnapshotSectionSetting {
  key: string;
  title: string;
  enabled: boolean;
  inEmail: boolean;
  fields: SnapshotFieldSetting[];
}

export interface SnapshotSettings {
  snapshotButtonEnabled: boolean;
  emailButtonEnabled: boolean;
  emailIntro: string;
  emailFooter: string;
  sections: SnapshotSectionSetting[];
  updatedAt?: string;
}

export interface EngagementSnapshot {
  title: string;
  scope: string;
  /** Engagement type when the snapshot covers a single engagement (scope === 'engagement'); '' otherwise. */
  engagementType: string;
  generatedAt: string;
  suggestedSubject: string;
  event?: EventInfoSection | null;
  summary: EngagementSummarySection;
  pipeline: PipelineSummarySection;
  attention: AttentionRow[];
  privateOffers: PrivateOfferRow[];
  settings: SnapshotSettings;
}

export interface EmailSummaryResponse {
  success: boolean;
  message: string;
  subject: string;
  to: string[];
  bodyHtml: string;
  delivered: boolean;
}

export interface DashboardInsights {
  activeEvents: number;
  activeEngagements: number;
  pendingFollowUps: number;
  pendingApprovals: number;
}
