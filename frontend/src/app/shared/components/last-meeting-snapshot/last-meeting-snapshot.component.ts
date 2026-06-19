import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  MeetingSessionRow,
  SnapshotHighlight,
  formatSessionDate,
  getSnapshotHighlights,
  truncateNotes
} from '@shared/utils/meeting-notes.util';

@Component({
  selector: 'app-last-meeting-snapshot',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './last-meeting-snapshot.component.html',
  styleUrl: './last-meeting-snapshot.component.scss'
})
export class LastMeetingSnapshotComponent {
  @Input() dealId = '';
  @Input() session: MeetingSessionRow | null = null;
  @Input() compact = false;
  @Input() showActions = true;

  formatSessionDate = formatSessionDate;

  get highlights(): SnapshotHighlight[] {
    return getSnapshotHighlights(this.session);
  }

  get excerpt(): string {
    return truncateNotes(this.session?.rawNotes || '', this.compact ? 120 : 260);
  }
}
