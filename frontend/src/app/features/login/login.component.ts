import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiService } from '@core/services/api.service';
import { ApiHealthService } from '@core/services/api-health.service';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private auth = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);
  health = inject(ApiHealthService);

  apiBase = environment.backendOrigin;
  googleEnabled = false;
  microsoftEnabled = false;

  email = '';
  password = '';
  remember = true;
  showPassword = false;
  error = '';
  loading = false;

  ngOnInit() {
    this.health.check();
    this.api.getAuthProviders().subscribe({
      next: (p: any) => {
        this.googleEnabled = p.google;
        this.microsoftEnabled = p.microsoft;
        this.health.online.set(true);
      },
      error: () => {
        this.googleEnabled = false;
        this.microsoftEnabled = false;
        this.health.online.set(false);
      }
    });
  }

  demoLogin() {
    this.error = '';
    this.loading = true;
    this.email = 'demo@marketplace.com';
    this.password = 'demo123';
    this.api.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => this.finishLogin(res),
      error: () => {
        this.auth.demoLogin(this.email);
        this.loading = false;
        void this.router.navigate(['/home']);
      }
    });
  }

  googleLogin() {
    if (!this.googleEnabled) {
      this.error = 'Google Sign-In is not configured yet. Use Demo Login or email/password.';
      return;
    }
    window.location.href = `${this.apiBase}/api/auth/google`;
  }

  microsoftLogin() {
    if (!this.microsoftEnabled) {
      this.error = 'Microsoft Sign-In is not configured yet. Use Demo Login or email/password.';
      return;
    }
    window.location.href = `${this.apiBase}/api/auth/microsoft`;
  }

  submit() {
    this.error = '';
    if (!this.email || !this.password) {
      this.error = 'Please enter your email and password.';
      return;
    }
    this.loading = true;
    this.api.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => this.finishLogin(res),
      error: (err) => {
        this.loading = false;
        if (err?.status === 0) {
          this.auth.demoLogin(this.email);
          void this.router.navigate(['/home']);
          return;
        }
        this.error = err?.error?.message || 'Sign in failed.';
        const status = err?.error?.status;
        if (status === 'pending_verification') void this.router.navigate(['/signup/verify-email']);
        if (status === 'awaiting_role') void this.router.navigate(['/signup/awaiting-role']);
      }
    });
  }

  private finishLogin(res: any) {
    this.loading = false;
    if (res?.success === false) {
      this.error = res.message || 'Sign in failed.';
      if (res.status === 'pending_verification') void this.router.navigate(['/signup/verify-email']);
      if (res.status === 'awaiting_role') void this.router.navigate(['/signup/awaiting-role']);
      return;
    }
    this.auth.login(
      res?.email || this.email,
      this.password,
      res?.name || this.email.split('@')[0] || 'Demo User',
      res?.role || 'Sales Representative',
      res?.token || 'demo-token',
      res?.status || 'approved',
      res?.provider || 'local'
    );
    void this.router.navigate(['/home']);
  }
}
