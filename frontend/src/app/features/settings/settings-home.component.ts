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
    }
  ];
}
