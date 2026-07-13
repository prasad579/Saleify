import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@core/services/auth.service';

/**
 * Attaches the signed-in user's name as the X-Acting-User header (audit-log attribution — display
 * only) and their session token as an Authorization bearer header on every API request. The token
 * is what the backend actually trusts to resolve the current tenant server-side (via
 * UserStore.FindByToken) — unlike X-Acting-User, that resolution is a real authorization boundary,
 * not just a display label, so it can't be a client-asserted value.
 */
export const actingUserInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const user = auth.user();
  const name = user?.name || user?.email;
  const headers: Record<string, string> = {};
  if (name) headers['X-Acting-User'] = name;
  if (user?.token) headers['Authorization'] = `Bearer ${user.token}`;
  if (Object.keys(headers).length) {
    req = req.clone({ setHeaders: headers });
  }
  return next(req);
};
