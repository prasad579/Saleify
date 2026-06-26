/**
 * Central configuration that drives engagement-type behaviour:
 * which flow screens apply, the final submit action, and whether a campaign tag is required.
 *
 * The catalog below is the built-in default. At startup, {@link applyEngagementConfigs} replaces
 * its contents with the tenant's customized settings from Settings → Engagement Types (loaded by
 * EngagementConfigService), so enabling/disabling a type and toggling its sections — e.g. turning
 * approvals off for a Free Trial — takes effect without a code change. The array reference is kept
 * stable (contents are spliced in place) so components that captured it keep seeing live values.
 */

export type ScreenKey = 'details' | 'products' | 'pricing' | 'meeting-notes' | 'approvals';
export type Visibility = 'yes' | 'optional' | 'no';
export type SubmitAction = 'submit' | 'complete' | 'convert-later';

export interface EngagementTypeConfig {
  type: string;
  blurb: string;
  /** Whether the type is offered at all. Disabled types are hidden from the create picker. */
  enabled: boolean;
  products: Visibility;
  pricing: Visibility;
  meetingNotes: Visibility;
  approvals: Visibility;
  submitLabel: string;
  submitAction: SubmitAction;
  tagRequired: boolean;
  /** Whether a marketplace must be selected. Lead-capture/internal types carry no marketplace offer. */
  marketplaceRequired: boolean;
}

/** Details always applies; the rest are per-config. Defaults match the backend seed catalog. */
export const ENGAGEMENT_CONFIGS: EngagementTypeConfig[] = [
  { type: 'Private Offer',           blurb: 'Marketplace private offer with full pricing & approvals', enabled: true, products: 'yes',      pricing: 'yes',      meetingNotes: 'yes', approvals: 'yes',      submitLabel: 'Submit to SaaSify',     submitAction: 'submit',        tagRequired: false, marketplaceRequired: true },
  { type: 'Free Trial',              blurb: 'Time-boxed trial — no charge until consumption limit',    enabled: true, products: 'yes',      pricing: 'optional', meetingNotes: 'yes', approvals: 'no',       submitLabel: 'Submit to SaaSify',     submitAction: 'submit',        tagRequired: true,  marketplaceRequired: true },
  { type: 'Workshop',                blurb: 'Customer enablement workshop',                            enabled: true, products: 'optional', pricing: 'no',       meetingNotes: 'yes', approvals: 'no',       submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'Hackathon',               blurb: 'Hands-on hackathon engagement',                           enabled: true, products: 'yes',      pricing: 'optional', meetingNotes: 'yes', approvals: 'optional', submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'POC',                     blurb: 'Proof of concept / pilot',                                enabled: true, products: 'yes',      pricing: 'optional', meetingNotes: 'yes', approvals: 'optional', submitLabel: 'Mark Completed',        submitAction: 'complete',      tagRequired: true,  marketplaceRequired: true },
  { type: 'Summit/Event Lead',       blurb: 'Lead captured at a summit or event',                      enabled: true, products: 'no',       pricing: 'no',       meetingNotes: 'yes', approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: true,  marketplaceRequired: true },
  { type: 'Internal Sales Activity', blurb: 'Internal sales activity (no marketplace offer)',          enabled: true, products: 'no',       pricing: 'no',       meetingNotes: 'yes', approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: false, marketplaceRequired: false },
  { type: 'External Source Lead',    blurb: 'Lead from an external source',                            enabled: true, products: 'no',       pricing: 'no',       meetingNotes: 'yes', approvals: 'no',       submitLabel: 'Save & Convert Later',  submitAction: 'convert-later', tagRequired: false, marketplaceRequired: false }
];

/** Replace the engagement catalog in place with tenant-customized settings (called at app startup). */
export function applyEngagementConfigs(configs: EngagementTypeConfig[]): void {
  if (!configs?.length) return;
  ENGAGEMENT_CONFIGS.splice(0, ENGAGEMENT_CONFIGS.length, ...configs);
}

/** Engagement types offered when creating an engagement — enabled ones only. */
export function enabledEngagementConfigs(): EngagementTypeConfig[] {
  return ENGAGEMENT_CONFIGS.filter(c => c.enabled);
}

export function getEngagementConfig(type: string | undefined | null): EngagementTypeConfig {
  return ENGAGEMENT_CONFIGS.find(c => c.type === type) ?? ENGAGEMENT_CONFIGS[0];
}

/** Maps a flow screen to the config property that controls its visibility. */
const SCREEN_VISIBILITY_KEY: Record<Exclude<ScreenKey, 'details'>, 'products' | 'pricing' | 'meetingNotes' | 'approvals'> = {
  'products': 'products',
  'pricing': 'pricing',
  'meeting-notes': 'meetingNotes',
  'approvals': 'approvals'
};

/** Whether a screen is shown for an engagement type. Details always applies. */
export function screenApplies(type: string, key: ScreenKey): boolean {
  if (key === 'details') return true;
  const cfg = getEngagementConfig(type);
  return cfg[SCREEN_VISIBILITY_KEY[key]] !== 'no';
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
