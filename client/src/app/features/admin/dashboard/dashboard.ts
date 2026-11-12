import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminService } from '../../../core/services/admin-service';
import { DisplayService } from '../../../core/services/display-service';
import { SettingsService } from '../../../core/services/settings-service';
import { AccountService } from '../../../core/services/account-service';
import { AdminTicket, Counter, Employee } from '../../../types/admin';
import { DEFAULT_SETTINGS, WorkSettings } from '../../../types/settings';
import { Icon } from '../../../shared/icon/icon';

type CounterState = 'Serving' | 'On break' | 'Free' | 'No employee' | 'Closed';

@Component({
  selector: 'app-dashboard',
  imports: [DatePipe, RouterLink, Icon],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private settingsService = inject(SettingsService);
  protected displayService = inject(DisplayService);
  protected accountService = inject(AccountService);
  private updateSub?: Subscription;
  private clock?: ReturnType<typeof setInterval>;

  protected tickets = signal<AdminTicket[]>([]);
  protected counters = signal<Counter[]>([]);
  protected employees = signal<Employee[]>([]);
  protected rules = signal<WorkSettings>({ ...DEFAULT_SETTINGS });
  protected now = signal(Date.now());
  protected error = signal('');

  protected greeting = computed(() => {
    const h = new Date(this.now()).getHours();
    return h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening';
  });

  private minutesSince(iso: string) {
    return Math.max(0, Math.floor((this.now() - new Date(iso).getTime()) / 60000));
  }

  // ---- stat cards ----
  protected waitingTickets = computed(() => this.tickets().filter((t) => t.status === 'InQueue'));
  protected waiting = computed(() => this.waitingTickets().length);
  protected newLast15 = computed(() => this.tickets().filter((t) => this.minutesSince(t.createdAt) < 15).length);
  protected oldest = computed(() => {
    const list = this.waitingTickets();
    if (!list.length) return null;
    const t = list.reduce((a, b) => (new Date(a.createdAt) < new Date(b.createdAt) ? a : b));
    return { ticket: t, minutes: this.minutesSince(t.createdAt) };
  });
  protected served = computed(() => this.tickets().filter((t) => t.status === 'Finished').length);
  protected noShows = computed(() => this.tickets().filter((t) => t.status === 'NoShow').length);

  // ---- alerts (same rules as the Queue page) ----
  protected alerts = computed(() => {
    const r = this.rules();
    const result: string[] = [];
    if (r.queueAlertWaiting > 0 && this.waiting() >= r.queueAlertWaiting)
      result.push(`${this.waiting()} people are waiting (limit ${r.queueAlertWaiting})`);
    const o = this.oldest();
    if (o && r.queueAlertMinutes > 0 && o.minutes >= r.queueAlertMinutes)
      result.push(`Longest wait is ${o.minutes} min (limit ${r.queueAlertMinutes} min) · ${o.ticket.code}`);
    return result;
  });

  // ---- counters: admin data + live display data + who works there ----
  protected counterCards = computed(() => {
    const live = new Map(this.displayService.counters().map((c) => [c.counterId, c]));
    return this.counters().map((c) => {
      const l = live.get(c.id);
      const employee = this.employees().find((e) => e.counterId === c.id);
      let state: CounterState;
      if (!c.isActive) state = 'Closed';
      else if (l?.isOnBreak) state = 'On break';
      else if (l?.currentTicketCode) state = 'Serving';
      else if (employee) state = 'Free';
      else state = 'No employee';
      return { id: c.id, name: c.name, state, code: l?.currentTicketCode ?? null, employee: employee?.username ?? null };
    });
  });
  protected openCounters = computed(() => this.counterCards().filter((c) => c.state !== 'Closed').length);
  protected onBreak = computed(() => this.counterCards().filter((c) => c.state === 'On break').length);

  // ---- queues by service ----
  protected queues = computed(() => {
    const r = this.rules();
    const list = this.displayService.queues();
    const max = Math.max(1, ...list.map((q) => q.waiting ?? 0));
    return list.map((q) => {
      const minutes = q.oldestWaitingSince ? this.minutesSince(q.oldestWaitingSince) : 0;
      const limit = r.queueAlertMinutes || 15;
      const level = !q.waiting ? 'none' : minutes >= limit ? 'high' : minutes >= limit / 2 ? 'mid' : 'low';
      return { ...q, minutes, level, width: ((q.waiting ?? 0) / max) * 100 };
    });
  });

  // ---- tickets per hour today, from opening to closing time ----
  protected hours = computed(() => {
    const r = this.rules();
    const open = parseInt(r.openTime, 10);
    const [closeH, closeM] = r.closeTime.split(':').map(Number);
    const lastWorkHour = Math.max(open, closeM > 0 ? closeH : closeH - 1);
    const counts = new Map<number, number>();
    for (const t of this.tickets()) {
      const h = new Date(t.createdAt).getHours();
      counts.set(h, (counts.get(h) ?? 0) + 1);
    }
    const first = Math.min(open, ...counts.keys());
    const last = Math.max(lastWorkHour, ...counts.keys());
    const nowHour = new Date(this.now()).getHours();
    const max = Math.max(1, ...counts.values());
    return Array.from({ length: last - first + 1 }, (_, i) => {
      const hour = first + i;
      const count = counts.get(hour) ?? 0;
      return {
        label: String(hour).padStart(2, '0'),
        count,
        height: count ? 8 + (count / max) * 92 : 0,
        current: hour === nowHour,
        future: hour > nowHour,
      };
    });
  });

  ngOnInit() {
    this.load();
    this.clock = setInterval(() => this.now.set(Date.now()), 30000);
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());
  }

  ngOnDestroy() {
    clearInterval(this.clock);
    this.updateSub?.unsubscribe();
  }

  load() {
    this.now.set(Date.now());
    this.settingsService.get().subscribe({ next: (s) => this.rules.set(s) });
    this.adminService.getTickets().subscribe({
      next: (t) => {
        this.tickets.set(t);
        this.error.set('');
      },
      error: () => this.error.set('Could not load tickets'),
    });
    this.adminService.getCounters().subscribe({ next: (c) => this.counters.set(c) });
    this.adminService.getEmployees().subscribe({ next: (e) => this.employees.set(e) });
  }
}
