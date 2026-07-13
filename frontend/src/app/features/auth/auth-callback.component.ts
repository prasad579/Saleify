import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, AuthStatus } from '@core/services/auth.service';

@Component({
  selector: 'app-auth-callback',
  standalone: true,
  template: `
    <div class="center-card">
      @if (error) {
        <div class="icon">⚠️</div>
        <h2>Sign-in issue</h2>
        <p>{{ errorMessage }}</p>
        @if (setupHint) { <p class="hint">{{ setupHint }}</p> }
        <button class="btn btn-primary" (click)="router.navigate(['/login'])">Back to Sign In</button>
      } @else {
        <div class="icon">⏳</div>
        <h2>Signing you in…</h2>
      }
    </div>
  `,
  styles: [`
    .center-card { max-width: 460px; margin: 100px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 48px; margin-bottom: 16px; }
    .hint { color: var(--muted); font-size: 13px; line-height: 1.5; }
  `]
})
export class AuthCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  router = inject(Router);

  error = '';
  errorMessage = '';
  setupHint = '';

  ngOnInit() {
    const params = this.route.snapshot.queryParamMap;
    const err = params.get('error');
    if (err) {
      this.error = err;
      this.errorMessage = this.mapError(err);
      this.setupHint = err.includes('not_configured')
        ? 'Add Google/Microsoft Client ID and Secret in backend/appsettings.json — see AUTH_SETUP.md'
        : '';
      return;
    }

    const token = params.get('token');
    const email = params.get('email');
    const name = params.get('name');
    const role = params.get('role') || 'Sales Representative';
    const provider = params.get('provider') || 'oauth';
    const status = (params.get('status') || 'approved') as AuthStatus;
    const tenantId = params.get('tenantId') || undefined;

    if (token && email && name) {
      this.auth.oauthLogin(token, email, name, role, provider, status, tenantId);
      this.router.navigate(['/home']);
      return;
    }

    this.error = 'missing_data';
    this.errorMessage = 'Sign-in did not return user data. Try again.';
  }

  private mapError(code: string): string {
    const map: Record<string, string> = {
      google_not_configured: 'Google Sign-In is not configured yet.',
      microsoft_not_configured: 'Microsoft Sign-In is not configured yet.',
      google_failed: 'Google sign-in was cancelled or failed.',
      microsoft_failed: 'Microsoft sign-in was cancelled or failed.',
      no_email: 'Could not read email from your account.',
    };
    return map[code] || 'Authentication failed.';
  }
}
