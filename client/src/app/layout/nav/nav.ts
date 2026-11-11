
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AccountService } from '../../core/services/account-service';
import { environment } from '../../../environments/environment';
import { Component, ElementRef, HostListener, inject, viewChild } from '@angular/core';
@Component({
  selector: 'app-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrl: './nav.css',
})
export class Nav {
  protected accountService = inject(AccountService);
  private router = inject(Router);
    private adminMenu = viewChild<ElementRef<HTMLDetailsElement>>('adminMenu');

  protected closeAdminMenu() {
    const el = this.adminMenu()?.nativeElement;
    if (el) el.open = false;
  }

  // close the dropdown when clicking anywhere outside of it
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    const el = this.adminMenu()?.nativeElement;
    if (el?.open && !el.contains(event.target as Node)) el.open = false;
  }
  logout() {
    this.accountService.logout();
   // this.router.navigateByUrl('/login');
  }
}