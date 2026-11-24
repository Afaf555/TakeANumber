import { Component, computed, HostListener, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../core/services/display-service';
import { EmployeeService } from '../../../core/services/employee-service';
import { EmployeeOverview, EmployeeTicket } from '../../../types/employee';
import { Icon } from '../../../shared/icon/icon';
import { ClientService } from '../../../core/services/client-service';
import { Service } from '../../../types/tickets';
@Component({
  selector: 'app-counter-panel',
  imports: [DatePipe, Icon],
  templateUrl: './counter-panel.html',
  styleUrl: './counter-panel.css',
})
export class CounterPanel implements OnInit, OnDestroy {
  private employeeService = inject(EmployeeService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;
  private clock?: ReturnType<typeof setInterval>;

  protected ticket = signal<EmployeeTicket | null>(null);
  protected overview = signal<EmployeeOverview | null>(null);
  protected error = signal('');
  protected onBreak = signal(false);
  protected busy = signal(false);
  protected now = signal(Date.now());

  private clientService = inject(ClientService);
  protected services = signal<Service[]>([]);
  protected transferTo = signal<number | null>(null);
  protected message = signal('');

  protected servingMinutes = computed(() => {
    const t = this.ticket();
    return t?.startedAt ? Math.max(0, Math.floor((this.now() - new Date(t.startedAt).getTime()) / 60000)) : 0;
  });
  protected waitedMinutes = computed(() => {
    const t = this.ticket();
    return t?.startedAt
      ? Math.max(0, Math.round((new Date(t.startedAt).getTime() - new Date(t.createdAt).getTime()) / 60000))
      : 0;
  });

  protected waiting = computed(() =>
    (this.overview()?.waiting ?? []).map((w) => ({
      ...w,
      minutes: Math.max(0, Math.floor((this.now() - new Date(w.createdAt).getTime()) / 60000)),
    })),
  );
  protected readonly limit = signal(15);

  ngOnInit() {
    this.load();
    this.clientService.getServices().subscribe({ next: (s) => this.services.set(s) });
    this.clock = setInterval(() => this.now.set(Date.now()), 15000);
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());

  }

  ngOnDestroy() {
    clearInterval(this.clock);
    this.updateSub?.unsubscribe();
  }

  @HostListener('document:keydown', ['$event'])
  onKey(e: KeyboardEvent) {
    const target = e.target as HTMLElement;
    if (e.ctrlKey || e.metaKey || e.altKey || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) return;
    if (this.onBreak() || this.busy()) return;
    const key = e.key.toLowerCase();
    if (key === ' ' && !this.ticket()) {
      e.preventDefault();
      this.callNext();
    } else if (key === 'f' && this.ticket()) {
      this.finish();
    } else if (key === 'n' && this.ticket()) {
      this.noShow();
    } else if (key === 'r' && this.ticket()) {
      this.recall();
    }
  }
  recall() {
    this.busy.set(true);
    this.employeeService.recall().subscribe({
      next: () => {
        this.error.set('');
        this.message.set('Ticket called again');
        setTimeout(() => this.message.set(''), 3000);
        this.busy.set(false);
      },
      error: (err) => {
        this.showError(err, 'Could not recall ticket');
        this.busy.set(false);
      },
    });
  }

  transfer() {
    const serviceId = this.transferTo();
    if (serviceId == null) return;
    this.busy.set(true);
    this.employeeService.transfer(serviceId).subscribe({
      next: () => {
        this.ticket.set(null);
        this.transferTo.set(null);
        this.error.set('');
        this.busy.set(false);
      },
      error: (err) => {
        this.showError(err, 'Could not transfer ticket');
        this.busy.set(false);
      },
    });
  }
  private load() {
    this.now.set(Date.now());
    this.employeeService.getCurrent().subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not load current ticket'),
    });
    this.employeeService.getStatus().subscribe({
      next: (s) => this.onBreak.set(s.isOnBreak),
    });
    this.employeeService.getOverview().subscribe({
      next: (o) => this.overview.set(o),
    });
  }

  startBreak() {
    this.employeeService.startBreak().subscribe({
      next: () => {
        this.onBreak.set(true);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not start break'),
    });
  }

  endBreak() {
    this.employeeService.endBreak().subscribe({
      next: () => {
        this.onBreak.set(false);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not end break'),
    });
  }

  callNext() {
    this.busy.set(true);
    this.employeeService.callNext().subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
        this.now.set(Date.now());
        this.busy.set(false);
      },
      error: (err) => {
        this.showError(err, 'Could not call next ticket');
        this.busy.set(false);
      },
    });
  }

  finish() {
    this.busy.set(true);
    this.employeeService.finish().subscribe({
      next: () => {
        this.ticket.set(null);
        this.error.set('');
        this.busy.set(false);
      },
      error: (err) => {
        this.showError(err, 'Could not finish ticket');
        this.busy.set(false);
      },
    });
  }

  noShow() {
    this.busy.set(true);
    this.employeeService.noShow().subscribe({
      next: () => {
        this.ticket.set(null);
        this.error.set('');
        this.busy.set(false);
      },
      error: (err) => {
        this.showError(err, 'Could not mark ticket as no-show');
        this.busy.set(false);
      },
    });
  }

  private showError(err: any, fallback: string) {
    this.error.set(typeof err.error === 'string' ? err.error : fallback);
  }
}
