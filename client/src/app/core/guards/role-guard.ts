import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account-service';

export const roleGuard: CanActivateFn = (route) => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  const user = accountService.currentUser();
  if (!user) return router.createUrlTree(['/login']);

  const roles = route.data['roles'] as string[] | undefined;
  if (!roles || roles.includes(user.role)) return true;

  return router.createUrlTree([accountService.homeFor(user.role)]);   
};