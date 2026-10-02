import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService),
    router = inject(Router);
  await auth.initialize();
  return auth.getToken() && auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
};
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService),
    router = inject(Router);
  await auth.initialize();
  return auth.getToken() && auth.isAuthenticated() ? router.createUrlTree(['/tasks']) : true;
};
