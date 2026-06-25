/**
 * Approval rules settings — mirrors the backend ApprovalRulesSettings contract.
 * Editable from Settings → Approval Rules so the discount / duration thresholds,
 * reviewers, and which engagement types each rule applies to are customizable.
 */

export type ApprovalConditionType =
  | 'discountGreaterThan'
  | 'durationMonthsGreaterThan'
  | 'marketplacePresent'
  | 'always';

export interface ApprovalRuleSetting {
  /** Stable identifier (finance, legal, marketplace) — not user-editable. */
  id: string;
  title: string;
  assignee: string;
  enabled: boolean;
  conditionType: ApprovalConditionType;
  /** Threshold for the numeric conditions (e.g. 15 for "discount > 15%"). */
  threshold: number;
  /** Engagement types this rule applies to. Empty = all types. */
  engagementTypes: string[];
}

export interface ApprovalRulesSettings {
  rules: ApprovalRuleSetting[];
  updatedAt?: string;
}
