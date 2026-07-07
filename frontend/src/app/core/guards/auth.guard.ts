import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isApproved()) {
    if (auth.user()?.role === 'Customer') {
      return router.createUrlTree(['/portal/requests']);
    }
    return true;
  }
  if (auth.isLoggedIn() && !auth.isApproved()) {
    return router.createUrlTree(['/signup/awaiting-role']);
  }
  return router.createUrlTree(['/login']);
};

/** Restricts the Customer Portal to logged-in, approved users with the Customer role. */
export const customerGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }
  if (!auth.isApproved()) {
    return router.createUrlTree(['/signup/awaiting-role']);
  }
  if (auth.user()?.role !== 'Customer') {
    return router.createUrlTree(['/home']);
  }
  return true;
};

export const guestGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (route.queryParamMap.get('logout') === '1') {
    auth.logout();
    return true;
  }
  if (auth.isLoggedIn() && auth.isApproved()) {
    return router.createUrlTree(['/home']);
  }
  return true;
};
