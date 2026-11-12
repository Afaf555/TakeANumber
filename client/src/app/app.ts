import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Nav } from './layout/nav/nav';
import { Sidebar } from './layout/sidebar/sidebar';
import { DisplayService } from './core/services/display-service';
import { AccountService } from './core/services/account-service';

// Screens that run full screen without any menu (TV display, ticket kiosk)
const BARE_PAGES = ['/display', '/take-ticket'];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Nav, Sidebar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private router = inject(Router);
  private accountService = inject(AccountService);

  private url = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map((e) => (e as NavigationEnd).urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  protected bare = computed(() => BARE_PAGES.some((p) => this.url().startsWith(p)));
  protected admin = computed(() => this.accountService.currentUser()?.role === 'Admin');

  // one router-outlet for every layout, only the classes around it change
  protected shellClass = computed(() =>
    !this.bare() && this.admin() ? 'min-h-screen lg:flex' : 'min-h-screen',
  );
  protected mainClass = computed(() => {
    if (this.bare()) return 'block';
    if (this.admin()) return 'block flex-1 min-w-0 w-full max-w-7xl mx-auto px-4 py-6 sm:px-8 lg:px-10 lg:py-8';
    return 'block w-full max-w-6xl mx-auto px-4 py-6 sm:px-6 sm:py-8';
  });

  constructor() {
    inject(DisplayService).startConnection();
  }
}
