import { Component, Input, inject } from '@angular/core';
import { NavigationHistoryService } from '@core/services/navigation-history.service';

/** In-app Back / Forward navigation bar, placed at the top and bottom of every page. */
@Component({
  selector: 'app-page-nav',
  standalone: true,
  template: `
    <div class="page-nav" [class.bottom]="position === 'bottom'">
      <button type="button" class="btn btn-outline btn-sm" [disabled]="!nav.canBack()" (click)="nav.back()" title="Go back">← Back</button>
      <button type="button" class="btn btn-outline btn-sm" [disabled]="!nav.canForward()" (click)="nav.forward()" title="Go forward">Forward →</button>
    </div>
  `,
  styles: [`
    .page-nav {
      display: flex;
      gap: 8px;
      margin-bottom: 14px;
    }
    .page-nav.bottom {
      margin-bottom: 0;
      margin-top: 20px;
      padding-top: 14px;
      border-top: 1px solid var(--border);
    }
    .btn[disabled] { opacity: 0.45; cursor: not-allowed; }
  `]
})
export class PageNavComponent {
  /** 'top' (default) or 'bottom' — controls spacing/border. */
  @Input() position: 'top' | 'bottom' = 'top';
  nav = inject(NavigationHistoryService);
}
