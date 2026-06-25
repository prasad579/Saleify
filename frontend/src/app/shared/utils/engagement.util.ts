/**
 * Central configuration that drives engagement-type behaviour:
 * which flow screens apply, the final submit action, and whether a campaign tag is required.
 */

export type ScreenKey = 'details' | 'products' | 'pricing' | 'meeting-notes' | 'approvals';
export type Visibility = 'yes' | 'optional' | 'no';
export type SubmitAction = 'submit' | 'complete' | 'convert-later';

export interface EngagementTypeConfig {
  type: string;
  blurb: string;
  products: Visibility;
  pricing: Visibility;
  approvals: Visibility;
  submitLabel: string;
  submitAction: SubmitAction;
  tagRequired: boolean;
  /** Whether a marketplace must be selected. Lead-capture/internal types carry no marketplace offer. */
  marketplaceRequired: boolean;
}

/** Details and Meeting Notes apply to every engagement type; the rest are per-config. */
export const ENGAGEMENT_CONFIGS: EngagementTypeConfig[] = [
  { type: 'Private Offer',          blurb: 'Marketplace private offer with full pricing & approvals', products: 'yes',      pricing: 'yes',      approvals: 'yes',      submitLabel: 'Submit to SaaSify',     submitAction: 'submit',        tagRequired: false, marketplaceRequired: true },
  { type: 'Free Trial',             blurb: 'Time-boxed trial — no charge until consumption limit',    products: 'yes',      pricing: 'optional', approvals: 'no',       submitLabel: 'Submit to SaaSify',     submitAction: 'submit',        tagRequired: true,  marketplaceRequired: true },
  { type: 'Workshop',               blurb: 'Customer enablement workshop',                            products: 'optional', pricing: 'no',       approvals: 'no',       submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'Hackathon',              blurb: 'Hands-on hackathon engagement',                           products: 'yes',      pricing: 'optional', approvals: 'optional', submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'POC',                    blurb: 'Proof of concept / pilot',                                products: 'yes',      pricing: 'optional', approvals: 'optional', submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'Summit/Event Lead',      blurb: 'Lead captured at a summit or event',                      products: 'no',       pricing: 'no',       approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: true,  marketplaceRequired: true },
  { type: 'Internal Sales Activity', blurb: 'Internal sales activity (no marketplace offer)',         products: 'no',       pricing: 'no',       approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: false, marketplaceRequired: false },
  { type: 'External Source Lead',   blurb: 'Lead from an external source',                            products: 'no',       pricing: 'no',       approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: false, marketplaceRequired: false }
];

const FALLBACK = ENGAGEMENT_CONFIGS[0];

export function getEngagementConfig(type: string | undefined | null): EngagementTypeConfig {
  return ENGAGEMENT_CONFIGS.find(c => c.type === type) ?? FALLBACK;
}

/** Whether a screen is shown for an engagement type. Details + Meeting Notes are always shown. */
export function screenApplies(type: string, key: ScreenKey): boolean {
  if (key === 'details' || key === 'meeting-notes') return true;
  const cfg = getEngagementConfig(type);
  return cfg[key] !== 'no';
}

/** Applicable screens, in flow order. */
export function applicableScreens(type: string): ScreenKey[] {
  const order: ScreenKey[] = ['details', 'products', 'pricing', 'meeting-notes', 'approvals'];
  return order.filter(k => screenApplies(type, k));
}

/** Stepper steps (label + key) for an engagement type, numbered by visible position. */
const SCREEN_LABELS: Record<ScreenKey, string> = {
  'details': 'Engagement Details',
  'products': 'Products',
  'pricing': 'Pricing',
  'meeting-notes': 'Meeting Notes',
  'approvals': 'Approvals'
};

export function stepperSteps(type: string): { key: ScreenKey; label: string }[] {
  return applicableScreens(type).map(k => ({ key: k, label: SCREEN_LABELS[k] }));
}

/** Route segment for a screen ('details' maps to the edit route). */
function routeSegment(dealId: string, key: ScreenKey): string {
  return key === 'details' ? `/deals/${dealId}/edit` : `/deals/${dealId}/${key}`;
}

export function nextScreenPath(type: string, dealId: string, current: ScreenKey): string | null {
  const screens = applicableScreens(type);
  const idx = screens.indexOf(current);
  if (idx < 0 || idx === screens.length - 1) return null;
  return routeSegment(dealId, screens[idx + 1]);
}

export function prevScreenPath(type: string, dealId: string, current: ScreenKey): string | null {
  const screens = applicableScreens(type);
  const idx = screens.indexOf(current);
  if (idx <= 0) return null;
  return routeSegment(dealId, screens[idx - 1]);
}

/** True when this is the last applicable screen — where the final Submit action lives. */
export function isLastScreen(type: string, current: ScreenKey): boolean {
  const screens = applicableScreens(type);
  return screens[screens.length - 1] === current;
}
