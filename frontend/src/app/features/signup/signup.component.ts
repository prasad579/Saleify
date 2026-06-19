import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { ApiService } from '@core/services/api.service';
import { ApiHealthService } from '@core/services/api-health.service';
import { apiErrorMessage } from '@shared/utils/deal-api.util';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.scss'
})
export class SignupComponent implements OnInit {
  private auth = inject(AuthService);
  private api = inject(ApiService);
  private router = inject(Router);

  apiBase = environment.backendOrigin;
  googleEnabled = false;
  microsoftEnabled = false;

  fullName = '';
  email = '';
  password = '';
  confirmPassword = '';
  agreed = false;
  error = '';

  ngOnInit() {
    this.api.getAuthProviders().subscribe({
      next: (p: any) => {
        this.googleEnabled = p.google;
        this.microsoftEnabled = p.microsoft;
      },
      error: () => {
        this.googleEnabled = false;
        this.microsoftEnabled = false;
      }
    });
  }

  googleSignup() {
    if (!this.googleEnabled) {
      this.error = 'Google Sign-In is not configured yet. Create an account with email/password or see AUTH_SETUP.md.';
      return;
    }
    window.location.href = `${this.apiBase}/api/auth/google`;
  }

  microsoftSignup() {
    if (!this.microsoftEnabled) {
      this.error = 'Microsoft Sign-In is not configured yet. Create an account with email/password or see AUTH_SETUP.md.';
      return;
    }
    window.location.href = `${this.apiBase}/api/auth/microsoft`;
  }

  submit() {
    if (!this.fullName || !this.email || !this.password) {
      this.error = 'Please fill in all required fields.';
      return;
    }
    if (this.password !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    if (!this.agreed) {
      this.error = 'Please agree to the Terms of Service.';
      return;
    }

    this.api.signup({ fullName: this.fullName, email: this.email, password: this.password }).subscribe({
      next: (res: any) => {
        if (res.success === false) {
          this.error = res.message || 'Could not create account.';
          return;
        }
        if (res.status === 'approved' && res.token) {
          this.auth.login(this.email, this.password, res.name, res.role, res.token, res.status, res.provider);
          this.router.navigate(['/home']);
          return;
        }
        this.auth.startSignup(this.fullName, this.email, this.password);
        this.router.navigate(['/signup/verify-email']);
      },
      error: (err) => {
        this.error = apiErrorMessage(err, 'Could not create account.');
      }
    });
  }
}
