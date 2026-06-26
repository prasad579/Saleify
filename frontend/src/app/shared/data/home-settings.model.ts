/**
 * Home / dashboard settings — mirrors the backend HomeSettings contract.
 * Editable from Settings → Home Dashboard to show/hide each home-page card.
 */

export interface HomeCardSetting {
  key: string;
  label: string;
  description: string;
  enabled: boolean;
}

export interface HomeSettings {
  cards: HomeCardSetting[];
  updatedAt?: string;
}
