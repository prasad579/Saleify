import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiService } from '@core/services/api.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="center-card">
      <div class="icon">✉️</div>
      <h2>Verify Your Email</h2>
      <p>We sent a verification link to <strong>{{ auth.user()?.email }}</strong>.</p>
      <p class="hint">For email/password signups, click below (demo). Google/Microsoft users skip this step.</p>
      <button class="btn btn-primary full" (click)="verify()">Verify Email (Demo)</button>
      <a routerLink="/login" class="back">← Back to Sign In</a>
    </div>
  `,
  styles: [`
    .center-card { max-width: 440px; margin: 80px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 64px; }
    .hint { color: var(--muted); font-size: 14px; }
    .full { width: 100%; margin-top: 16px; }
    .back { display: block; margin-top: 16px; color: var(--primary); }
  `]
})
export class VerifyEmailComponent {
  auth = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);

  verify() {
    const email = this.auth.user()?.email;
    if (email) {
      this.api.verifyEmail(email).subscribe();
    }
    this.auth.markEmailVerified();
    this.router.navigate(['/signup/email-verified']);
  }
}
