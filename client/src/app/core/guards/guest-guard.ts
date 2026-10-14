import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account-service';

export const guestGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  const user = accountService.currentUser();
  if (!user) return true;

  return router.createUrlTree([accountService.homeFor(user.role)]);
};