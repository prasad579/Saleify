import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

interface SettingsCard {
  path: string;
  icon: string;
  title: string;
  description: string;
}

@Component({
  selector: 'app-settings-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './settings-home.component.html',
  styleUrl: './settings-home.component.scss'
})
export class SettingsHomeComponent {
  cards: SettingsCard[] = [
    {
      path: '/settings/home',
      icon: '🏠',
      title: 'Home Dashboard',
      description: 'Choose which cards appear on the home page — stats, insights, tags, open engagements, recent activity, tasks, and reminders.'
    },
    {
      path: '/settings/alerts',
      icon: '🔔',
      title: 'Alerts & Reminders',
      description: 'Turn the home “needs attention” alert and “upcoming” card on or off, set how many days ahead to look, and choose which items count.'
    },
    {
      path: '/settings/engagement-types',
      icon: '🧭',
      title: 'Engagement Types',
      description: 'Enable or disable engagement types and choose which sections (products, pricing, meeting notes, approvals) apply to each — e.g. turn approvals off for a Free Trial.'
    },
    {
      path: '/settings/campaign-events',
      icon: '🎟️',
      title: 'Campaign / Event Tags',
      description: 'Add, edit, pause, or remove the event tags engagements can be associated with, and track conversion.'
    },
    {
      path: '/settings/people',
      icon: '👥',
      title: 'People',
      description: 'Manage the engagement owners — add people, set their role, enable/disable them, and restrict who can own which engagement types.'
    },
    {
      path: '/settings/playbooks',
      icon: '⚙️',
      title: 'Engagement Playbooks',
      description: 'Customize the "what’s next" guidance shown for each engagement type — next steps, talking points, timeline.'
    },
    {
      path: '/settings/snapshot',
      icon: '📋',
      title: 'Snapshot & Email',
      description: 'Turn the snapshot/email buttons on or off and choose which sections and fields appear in the snapshot and email.'
    },
    {
      path: '/settings/approvals',
      icon: '✅',
      title: 'Approval Rules',
      description: 'Decide which reviews an engagement requires — discount/duration thresholds, reviewers, and which engagement types each rule applies to.'
    },
    {
      path: '/audit-log',
      icon: '📜',
      title: 'Audit Log',
      description: 'See every change made across the application — who changed what, when, and the details — for engagements, pricing, approvals, and settings.'
    }
  ];
}
