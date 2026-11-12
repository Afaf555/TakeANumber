import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AccountService } from '../../core/services/account-service';
import { Icon } from '../../shared/icon/icon';

// Top bar for employees, clients and guests (admins use the sidebar)
@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive, Icon],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);

  logout() {
    this.accountService.logout();
  }
}
