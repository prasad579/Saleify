import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@core/services/auth.service';

/**
 * Attaches the signed-in user's name as the X-Acting-User header on API requests so the backend
 * can attribute audit-log entries to a real person (the SPA authenticates client-side, so the
 * server has no other reliable signal of who is acting).
 */
export const actingUserInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const user = auth.user();
  const name = user?.name || user?.email;
  if (name) {
    req = req.clone({ setHeaders: { 'X-Acting-User': name } });
  }
  return next(req);
};
