import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminService } from '../../../core/services/admin-service';
import { DisplayService } from '../../../core/services/display-service';
import { AdminTicket } from '../../../types/admin';
import { SettingsService } from '../../../core/services/settings-service';
import { DEFAULT_SETTINGS, WorkSettings } from '../../../types/settings';
@Component({
  imports: [DatePipe],
  selector: 'app-queue-overview',
  styleUrl: './queue-overview.css',
  templateUrl: './queue-overview.html',
})
export class QueueOverview implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;

  protected tickets = signal<AdminTicket[]>([]);
  protected showFinished = signal(false);
  protected error = signal('');

  protected visible = computed(() =>
    this.showFinished()
      ? this.tickets()
      : this.tickets().filter((t) => t.status === 'InQueue' || t.status === 'InProgress')
  );
  protected page = signal(0);
  protected readonly pageSize = 10;
  protected pages = computed(() => Math.max(1, Math.ceil(this.visible().length / this.pageSize)));
  protected current = computed(() => Math.min(this.page(), this.pages() - 1));
  protected paged = computed(() => {
    const start = this.current() * this.pageSize;
    return this.visible().slice(start, start + this.pageSize);
  });
  protected waiting = computed(() => this.tickets().filter((t) => t.status === 'InQueue').length);
  private settingsService = inject(SettingsService);
  private clock?: ReturnType<typeof setInterval>;

  protected rules = signal<WorkSettings>({ ...DEFAULT_SETTINGS });
  protected now = signal(Date.now());

  protected longestWait = computed(() => {
    const times = this.tickets()
      .filter((t) => t.status === 'InQueue')
      .map((t) => new Date(t.createdAt).getTime());
    return times.length ? Math.max(0, Math.floor((this.now() - Math.min(...times)) / 60000)) : 0;
  });

  protected alerts = computed(() => {
    const r = this.rules();
    const result: string[] = [];
    if (r.queueAlertWaiting > 0 && this.waiting() > r.queueAlertWaiting)
      result.push(`${this.waiting()} people are waiting (limit ${r.queueAlertWaiting})`);
    if (r.queueAlertMinutes > 0 && this.longestWait() > r.queueAlertMinutes)
      result.push(`Longest wait is ${this.longestWait()} min (limit ${r.queueAlertMinutes} min)`);
    return result;
  });
  ngOnInit() {
    this.settingsService.get().subscribe({ next: (s) => this.rules.set(s) });
    this.clock = setInterval(() => this.now.set(Date.now()), 30000);
    this.load();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());
  }

  ngOnDestroy() {
    clearInterval(this.clock);
    this.updateSub?.unsubscribe();
  }

  load() {
    this.adminService.getTickets().subscribe({
      next: (tickets) => {
        this.tickets.set(tickets);
        this.error.set('');
      },
      error: () => this.error.set('Could not load tickets'),
    });
  }
}