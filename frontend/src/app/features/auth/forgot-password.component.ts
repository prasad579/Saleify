import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="center-card">
      <div class="icon">🔑</div>
      <h2>Forgot Password?</h2>
      <p>Enter your email and we'll send a reset link.</p>
      <input type="email" [(ngModel)]="email" placeholder="you@company.com" />
      <button class="btn btn-primary full" (click)="send()">Send Reset Link</button>
      <a routerLink="/login" class="back">← Back to Sign In</a>
    </div>
  `,
  styles: [`
    .center-card { max-width: 400px; margin: 80px auto; text-align: center; padding: 32px; background: white; border-radius: 16px; border: 1px solid var(--border); }
    .icon { font-size: 48px; }
    input { width: 100%; padding: 12px; border: 1px solid var(--border); border-radius: 8px; margin: 16px 0; box-sizing: border-box; }
    .full { width: 100%; }
    .back { display: block; margin-top: 16px; color: var(--primary); }
  `]
})
export class ForgotPasswordComponent {
  private router = inject(Router);
  email = '';

  send() {
    this.router.navigate(['/reset-sent'], { queryParams: { email: this.email } });
  }
}
