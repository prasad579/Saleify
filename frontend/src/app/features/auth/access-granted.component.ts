import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-access-granted',
  standalone: true,
  template: `
    <div class="center-card">
      <div class="icon">🎉</div>
      <h2>Welcome to Marketplace Copilot!</h2>
      <p>Your role has been assigned.</p>
      <div class="role-badge">Role: {{ auth.user()?.role }}</div>
      <button class="btn btn-primary full" (click)="go()">Go to Dashboard →</button>
    </div>
  `,
  styles: [`
    .center-card { max-width: 440px; margin: 80px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 64px; }
    .role-badge { background: #ede9fe; color: #5b21b6; padding: 12px 20px; border-radius: 8px; margin: 20px 0; font-weight: 600; }
    .full { width: 100%; }
  `]
})
export class AccessGrantedComponent {
  auth = inject(AuthService);
  private router = inject(Router);

  go() {
    this.router.navigate(['/home']);
  }
}
