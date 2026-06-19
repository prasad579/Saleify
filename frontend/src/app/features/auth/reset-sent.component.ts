import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-reset-sent',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="center-card">
      <div class="icon">✉️ ✅</div>
      <h2>Reset Link Sent</h2>
      <p>We've sent a password reset link to <strong>{{ email }}</strong>.</p>
      <p class="hint">Check your spam folder or <a routerLink="/forgot-password">try again</a>.</p>
      <a routerLink="/login" class="btn btn-primary full">Back to Sign In</a>
    </div>
  `,
  styles: [`
    .center-card { max-width: 420px; margin: 80px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 56px; margin-bottom: 16px; }
    .hint { color: var(--muted); font-size: 14px; }
    .full { display: inline-block; margin-top: 20px; padding: 12px 24px; text-decoration: none; }
  `]
})
export class ResetSentComponent implements OnInit {
  private route = inject(ActivatedRoute);
  email = 'your email';

  ngOnInit() {
    this.email = this.route.snapshot.queryParamMap.get('email') || 'your email';
  }
}
