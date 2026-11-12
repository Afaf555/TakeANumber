import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AccountService } from "../services/account-service";
export const clientOnlyGuard:CanActivateFn=()=>{
    const accountSerice=inject(AccountService);
    const router=inject(Router);
    return accountSerice.currentUser()?.role==='Admin'
    ?router.createUrlTree(['/dashboard'])
    :true;
};