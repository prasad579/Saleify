import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-email-verified',
  standalone: true,
  template: `
    <div class="center-card">
      <div class="icon success">✅</div>
      <h2>Email Verified!</h2>
      <p>Your email is verified. Your account is now under administrative review.</p>
      <p class="hint">An administrator must assign your role before you can access the dashboard.</p>
      <button class="btn btn-primary full" (click)="continue()">Continue</button>
    </div>
  `,
  styles: [`
    .center-card { max-width: 440px; margin: 80px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 72px; }
    .success { color: var(--success); }
    .hint { color: var(--muted); font-size: 14px; }
    .full { width: 100%; margin-top: 20px; }
  `]
})
export class EmailVerifiedComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  continue() {
    this.auth.markAwaitingRole();
    this.router.navigate(['/signup/awaiting-role']);
  }
}
