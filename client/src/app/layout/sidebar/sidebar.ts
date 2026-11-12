import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AccountService } from '../../core/services/account-service';
import { DisplayService } from '../../core/services/display-service';
import { Icon } from '../../shared/icon/icon';

type NavLink = { path: string; label: string; icon: string; badge?: boolean };

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, Icon],
  templateUrl: './sidebar.html',
  host: { class: 'block lg:w-64 lg:shrink-0' },
})
export class Sidebar {
  protected accountService = inject(AccountService);
  private displayService = inject(DisplayService);

  protected open = signal(false); // mobile menu

  protected readonly groups: { title: string; links: NavLink[] }[] = [
    {
      title: 'Live',
      links: [
        { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
        { path: '/queue-overview', label: 'Queue', icon: 'queue', badge: true },
        { path: '/display', label: 'Display', icon: 'display' },
      ],
    },
    {
      title: 'Setup',
      links: [
        { path: '/manage-services', label: 'Services', icon: 'services' },
        { path: '/manage-counters', label: 'Counters', icon: 'counters' },
        { path: '/manage-employees', label: 'Employees', icon: 'employees' },
      ],
    },
    {
      title: 'Insights',
      links: [
        { path: '/reports', label: 'Reports', icon: 'reports' },
        { path: '/workforce', label: 'Workforce', icon: 'workforce' },
        { path: '/calendar', label: 'Calendar', icon: 'calendar' },
        { path: '/settings', label: 'Settings', icon: 'settings' },
      ],
    },
  ];

  // number shown next to "Queue": everybody waiting right now
  protected waiting = computed(() => this.displayService.queues().reduce((sum, q) => sum + (q.waiting ?? 0), 0));

  protected initial = computed(() => (this.accountService.currentUser()?.username ?? '?').charAt(0).toUpperCase());

  logout() {
    this.open.set(false);
    this.accountService.logout();
  }
}
