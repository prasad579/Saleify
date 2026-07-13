import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';

export type AuthStatus = 'none' | 'pending_verification' | 'email_verified' | 'awaiting_role' | 'approved';

export interface UserSession {
  name: string;
  email: string;
  role: string;
  token: string;
  status: AuthStatus;
  provider?: string;
  company?: string;
  tenantId?: string;
  tenantName?: string;
}

/** Fallback tenant when the server doesn't return one yet — mirrors Tenant.DefaultTenantId. */
const DEFAULT_TENANT_ID = 'TEN-1';
const DEFAULT_TENANT_NAME = 'SaaSify';

@Injectable({ providedIn: 'root' })
export class AuthService {
  user = signal<UserSession | null>(this.load());

  login(email: string, _password: string, name = 'Srinivas K', role = 'Sales Representative', token = 'demo-token', status: AuthStatus = 'approved', provider = 'local', company = '', tenantId = DEFAULT_TENANT_ID, tenantName = DEFAULT_TENANT_NAME) {
    this.setSession({ name, email, role, token, status, provider, company, tenantId, tenantName });
  }

  oauthLogin(token: string, email: string, name: string, role: string, provider: string, status: AuthStatus = 'approved', tenantId = DEFAULT_TENANT_ID) {
    this.setSession({ name, email, role, token, status, provider, tenantId });
  }

  startSignup(fullName: string, email: string, _password: string) {
    this.setSession({ name: fullName, email, role: '', token: '', status: 'pending_verification', provider: 'local' });
  }

  markEmailVerified() {
    const u = this.user();
    if (!u) return;
    this.setSession({ ...u, status: 'email_verified' });
  }

  markAwaitingRole() {
    const u = this.user();
    if (!u) return;
    this.setSession({ ...u, status: 'awaiting_role' });
  }

  approveRole(role = 'Sales Representative', token?: string) {
    const u = this.user();
    if (!u) return;
    this.setSession({ ...u, role, token: token || u.token || 'demo-token', status: 'approved' });
  }

  logout() {
    localStorage.removeItem('mc_user');
    this.user.set(null);
  }

  signOut(router: Router) {
    this.logout();
    void router.navigateByUrl('/login', { replaceUrl: true });
  }

  demoLogin(email = 'demo@marketplace.com') {
    this.setSession({
      name: 'Srinivas K',
      email,
      role: 'Sales Representative',
      token: 'demo-local-session',
      status: 'approved',
      provider: 'local',
      tenantId: DEFAULT_TENANT_ID,
      tenantName: DEFAULT_TENANT_NAME
    });
  }

  customerDemoLogin(email = 'customer@acme.com') {
    this.setSession({
      name: 'John Ramesh',
      email,
      role: 'Customer',
      token: 'demo-local-session',
      status: 'approved',
      provider: 'local',
      company: 'Acme Corporation'
    });
  }

  isLoggedIn() {
    return !!this.user();
  }

  isApproved() {
    return this.user()?.status === 'approved';
  }

  private setSession(session: UserSession) {
    localStorage.setItem('mc_user', JSON.stringify(session));
    // A fresh approved sign-in re-shows the home "needs attention" alert (dismissal is per-login).
    if (session.status === 'approved') {
      try { localStorage.removeItem('mc_home_alert_dismissed'); } catch { /* ignore */ }
    }
    this.user.set(session);
  }

  private load(): UserSession | null {
    try {
      const raw = localStorage.getItem('mc_user');
      if (!raw) return null;
      const parsed = JSON.parse(raw) as Partial<UserSession>;
      if (!parsed?.email) {
        localStorage.removeItem('mc_user');
        return null;
      }
      return {
        name: parsed.name ?? '',
        email: parsed.email,
        role: parsed.role ?? '',
        token: parsed.token ?? '',
        status: parsed.status || 'approved',
        provider: parsed.provider,
        company: parsed.company,
        tenantId: parsed.tenantId,
        tenantName: parsed.tenantName
      };
    } catch {
      localStorage.removeItem('mc_user');
      return null;
    }
  }
}
