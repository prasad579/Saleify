import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiService } from '@core/services/api.service';

@Component({
  selector: 'app-awaiting-role',
  standalone: true,
  imports: [],
  template: `
    <div class="center-card">
      <div class="icon">🕐 👤</div>
      <h2>Account Under Review</h2>
      <p>Hi <strong>{{ auth.user()?.name }}</strong>, your account is waiting for an administrator to assign a role.</p>
      <p class="hint">You cannot access deals or the dashboard until your role is approved.</p>
      <button class="btn btn-primary full" (click)="simulateAdmin()">Simulate Admin Approval (Demo)</button>
      <button type="button" class="back" (click)="signOut()">Sign out</button>
    </div>
  `,
  styles: [`
    .center-card { max-width: 460px; margin: 80px auto; text-align: center; padding: 40px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 48px; }
    .hint { color: var(--muted); font-size: 14px; }
    .full { width: 100%; margin-top: 16px; }
    .back { display: block; margin-top: 16px; color: var(--primary); cursor: pointer; }
  `]
})
export class AwaitingRoleComponent {
  auth = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);

  simulateAdmin() {
    const email = this.auth.user()?.email;
    if (email) {
      this.api.approveRole(email, 'Sales Representative').subscribe({
        next: (res: any) => {
          this.auth.approveRole(res.role, res.token);
          this.router.navigate(['/signup/access-granted']);
        },
        error: () => {
          this.auth.approveRole('Sales Representative');
          this.router.navigate(['/signup/access-granted']);
        }
      });
    } else {
      this.auth.approveRole('Sales Representative');
      this.router.navigate(['/signup/access-granted']);
    }
  }

  signOut() {
    this.auth.signOut(this.router);
  }
}
