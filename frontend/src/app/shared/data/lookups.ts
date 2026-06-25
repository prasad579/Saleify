export const COUNTRIES = [
  'United States', 'United Kingdom', 'Canada', 'India', 'Australia',
  'Germany', 'France', 'Netherlands', 'Singapore', 'Japan',
  'United Arab Emirates', 'Brazil', 'Mexico', 'Ireland', 'Switzerland',
  'Sweden', 'Norway', 'Denmark', 'Finland', 'Spain', 'Italy',
  'South Korea', 'New Zealand', 'South Africa', 'Israel'
];

export const SAAS_INDUSTRIES = [
  'Software & SaaS',
  'Cloud Infrastructure',
  'Cybersecurity',
  'FinTech',
  'HealthTech',
  'EdTech',
  'MarTech',
  'HR Tech',
  'Data & Analytics',
  'AI & Machine Learning',
  'DevOps & Platform Engineering',
  'IT Services & Consulting',
  'E-commerce & Retail Tech',
  'Telecommunications',
  'Manufacturing & Industrial SaaS',
  'Government & Public Sector',
  'Media & Entertainment',
  'Legal Tech',
  'PropTech',
  'InsurTech'
];

export const DEAL_TYPES = ['New Deal', 'Renewal'];

export const MARKETPLACES = ['AWS', 'Azure', 'GCP'];

/** Per-marketplace billing account fields — shown only for the marketplaces a deal targets. */
export interface MarketplaceAccountField {
  marketplace: string;
  label: string;
  placeholder: string;
}

export const MARKETPLACE_ACCOUNTS: MarketplaceAccountField[] = [
  { marketplace: 'AWS', label: 'AWS Account ID', placeholder: '12-digit AWS account ID' },
  { marketplace: 'Azure', label: 'Azure Billing Account ID', placeholder: 'Azure billing account / subscription ID' },
  { marketplace: 'GCP', label: 'GCP Billing Account ID', placeholder: 'GCP billing account ID' }
];

export const ENGAGEMENT_TYPES = ['Workshop', 'Hackathon', 'Summit', 'POC', 'Funding', 'Free Trial', 'Private Offer', 'Direct Deal'];

export const DEAL_OWNERS = ['Srinivas K', 'Priya Sharma', 'Arjun Mehta', 'Neha Gupta'];

export const PRIORITIES = ['High', 'Medium', 'Low'];

export const OFFER_TYPES = ['Free Trial', 'POC / Pilot', 'Direct Private Offer', 'Reseller Private Offer', 'Renewal'];

/**
 * Offer types relevant to each engagement type. Pricing shows only these and
 * auto-selects the first. Types whose pricing screen never applies
 * (Workshop / Summit / Internal / External) fall back to the full list but are
 * never reached.
 */
export function offerTypesForEngagement(engagementType: string | undefined | null): string[] {
  switch ((engagementType || '').trim()) {
    case 'Free Trial': return ['Free Trial'];
    case 'POC': return ['POC / Pilot', 'Free Trial', 'Direct Private Offer'];
    case 'Hackathon': return ['POC / Pilot', 'Free Trial', 'Direct Private Offer'];
    case 'Private Offer': return ['Direct Private Offer', 'Reseller Private Offer', 'Renewal'];
    default: return [...OFFER_TYPES];
  }
}

/** Quick-pick trial lengths; users can also enter a custom number of days. */
export const TRIAL_DAY_OPTIONS = [7, 14, 30];

/** Engagement types that imply a free trial (no money) when creating the deal. */
export const FREE_TRIAL_ENGAGEMENTS = ['Free Trial'];

/** Free trial / POC / pilot — time-boxed, no-money offer; skips discount and full contract terms. */
export function isNoMoneyOffer(offerType: string): boolean {
  return /free trial|poc|pilot/i.test(offerType || '');
}

/** Direct/Reseller private offer or renewal — shows the full contract & pricing section. */
export function isContractOffer(offerType: string): boolean {
  return /private offer|renewal/i.test(offerType || '');
}

/**
 * An engagement owner. Managed from Settings → People (enable/disable, role, and which
 * engagement types they can own). `source` marks the origin ("manual" today; a tenant /
 * directory sync can populate these later).
 */
export interface Person {
  id: string;
  name: string;
  email: string;
  role: string;
  enabled: boolean;
  /** Engagement types this person can own; empty = eligible for all types. */
  engagementTypes: string[];
  source: string;
}

export interface CampaignEvent {
  id: string;
  name: string;
  marketplace: string;
  startDate: string;
  endDate: string;
  description: string;
  status: 'Upcoming' | 'Active' | 'Completed' | string;
  paused?: boolean;
  createdAt?: string;
}

/** Sentinel tag for engagements deliberately not tied to any event/campaign. */
export const NO_EVENT_TAG = { id: 'NONE', name: 'Not part of any event' };

/** Derive event status on the client (mirrors the backend rule) for instant feedback. */
export function eventStatus(startDate: string, endDate: string): 'Upcoming' | 'Active' | 'Completed' {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const start = startDate ? new Date(startDate) : null;
  const end = endDate ? new Date(endDate) : null;
  if (start && today < start) return 'Upcoming';
  if (end && today > end) return 'Completed';
  return 'Active';
}
