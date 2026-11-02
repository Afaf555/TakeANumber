import { inject } from "@angular/core";
import { CanActivateFn,Router } from "@angular/router";
import { AccountService } from "../services/account-service";

export const adminRedirectGuard: CanActivateFn=()=>{
    const accountService=inject(AccountService);
    const router=inject(Router);
    return accountService.currentUser()?.role==='Admin'
    ?router.createUrlTree(['/display'])
    :router.createUrlTree(['/take-ticket'])
};