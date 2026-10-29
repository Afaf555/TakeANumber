import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AccountService } from '../../core/services/account-service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);
  private router = inject(Router);
  protected closeTime = String(environment.closeHour).padStart(2, '0') + ':00';

  logout() {
    this.accountService.logout();
   // this.router.navigateByUrl('/login');
  }
}