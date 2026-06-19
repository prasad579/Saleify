import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn() && auth.isApproved()) {
    return true;
  }
  if (auth.isLoggedIn() && !auth.isApproved()) {
    return router.createUrlTree(['/signup/awaiting-role']);
  }
  return router.createUrlTree(['/login']);
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
