/**
 * Alerts & reminders settings — mirrors the backend AttentionSettings contract.
 * Controls the home "needs attention" alert and the "upcoming this week" card.
 */

export interface AttentionSettings {
  alertEnabled: boolean;
  upcomingEnabled: boolean;
  upcomingWindowDays: number;
  includeTasks: boolean;
  includeReminders: boolean;
  includeEngagements: boolean;
  updatedAt?: string;
}
