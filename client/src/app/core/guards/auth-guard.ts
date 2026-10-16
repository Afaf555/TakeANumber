import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account-service';

export const authGuard: CanActivateFn = () => {
    const accountService = inject(AccountService);
  const router = inject(Router);

  return accountService.currentUser() ? true : router.createUrlTree(['/login']);
};
