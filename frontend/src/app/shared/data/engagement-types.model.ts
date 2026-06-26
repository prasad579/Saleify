/**
 * Engagement-type settings — mirrors the backend EngagementTypeSettings contract.
 * Editable from Settings → Engagement Types so each type can be enabled/disabled and its
 * applicable sections (Products / Pricing / Meeting Notes / Approvals) customized.
 */

import { SubmitAction, Visibility } from '@shared/utils/engagement.util';

export interface EngagementTypeSetting {
  type: string;
  blurb: string;
  enabled: boolean;
  products: Visibility;
  pricing: Visibility;
  meetingNotes: Visibility;
  approvals: Visibility;
  submitLabel: string;
  submitAction: SubmitAction;
  tagRequired: boolean;
  marketplaceRequired: boolean;
}

export interface EngagementTypeSettings {
  types: EngagementTypeSetting[];
  updatedAt?: string;
}
