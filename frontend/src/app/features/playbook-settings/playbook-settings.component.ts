import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '@core/services/api.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';

interface PlaybookEdit {
  engagementType: string;
  headline: string;
  nextStepsText: string;
  talkingPointsText: string;
  timeline: string;
  updatedAt?: string;
  savedFlash?: boolean;
}

@Component({
  selector: 'app-playbook-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './playbook-settings.component.html',
  styleUrl: './playbook-settings.component.scss'
})
export class PlaybookSettingsComponent implements OnInit {
  private api = inject(ApiService);

  playbooks: PlaybookEdit[] = [];
  loading = true;
  error = '';
  success = '';

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.api.getPlaybooks().subscribe({
      next: (data: any) => {
        this.playbooks = (Array.isArray(data) ? data : []).map((p: any) => this.toEdit(p));
        this.loading = false;
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not load playbooks.');
        this.loading = false;
      }
    });
  }

  private toEdit(p: any): PlaybookEdit {
    return {
      engagementType: p.engagementType,
      headline: p.headline || '',
      nextStepsText: (p.nextSteps || []).join('\n'),
      talkingPointsText: (p.talkingPoints || []).join('\n'),
      timeline: p.timeline || '',
      updatedAt: p.updatedAt
    };
  }

  save(pb: PlaybookEdit) {
    this.error = '';
    this.success = '';
    const payload = {
      engagementType: pb.engagementType,
      headline: pb.headline,
      nextSteps: pb.nextStepsText.split('\n').map(s => s.trim()).filter(Boolean),
      talkingPoints: pb.talkingPointsText.split('\n').map(s => s.trim()).filter(Boolean),
      timeline: pb.timeline
    };
    this.api.savePlaybook(payload).subscribe({
      next: (res: any) => {
        pb.updatedAt = res?.updatedAt;
        pb.savedFlash = true;
        setTimeout(() => pb.savedFlash = false, 2500);
      },
      error: (err) => { this.error = apiErrorMessage(err, 'Could not save playbook.'); }
    });
  }

  resetAll() {
    this.error = '';
    this.api.resetPlaybooks().subscribe({
      next: (data: any) => {
        this.playbooks = (Array.isArray(data) ? data : []).map((p: any) => this.toEdit(p));
        this.success = 'Playbooks reset to defaults.';
        setTimeout(() => this.success = '', 3000);
      },
      error: (err) => { this.error = apiErrorMessage(err, 'Could not reset playbooks.'); }
    });
  }
}
