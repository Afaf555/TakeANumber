import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AccountService } from '../../../core/services/account-service';
import { ClientService } from '../../../core/services/client-service';
import { DisplayService } from '../../../core/services/display-service';
import { Service, Ticket } from '../../../types/tickets';
import { Icon } from '../../../shared/icon/icon';

// tile colors for the services, in order
const TILE_COLORS = [
  'bg-blue-100 text-blue-700',
  'bg-emerald-100 text-emerald-700',
  'bg-amber-100 text-amber-800',
  'bg-violet-100 text-violet-700',
  'bg-rose-100 text-rose-700',
  'bg-cyan-100 text-cyan-800',
];
const RESET_SECONDS = 10;

@Component({
  selector: 'app-take-ticket',
  imports: [DatePipe, RouterLink, Icon],
  templateUrl: './take-ticket.html',
  styleUrl: './take-ticket.css',
})
export class TakeTicket implements OnInit, OnDestroy {
  private clientService = inject(ClientService);
  private displayService = inject(DisplayService);
  protected accountService = inject(AccountService);
  private updateSub?: Subscription;

  protected services = signal<Service[]>([]);
  protected ticket = signal<Ticket | null>(null);
  protected error = signal('');
  protected now = signal(Date.now());
  protected countdown = signal(RESET_SECONDS);
  private clock?: ReturnType<typeof setInterval>;
  private resetTimer?: ReturnType<typeof setInterval>;

  // services with their tile color and how many people wait for them
  protected tiles = computed(() => {
    const waiting = new Map(this.displayService.queues().map((q) => [q.serviceId, q.waiting ?? 0]));
    return this.services().map((s, i) => ({
      ...s,
      color: TILE_COLORS[i % TILE_COLORS.length],
      waiting: waiting.get(s.id) ?? 0,
    }));
  });

  ngOnInit() {
    this.loadServices();
    this.clock = setInterval(() => this.now.set(Date.now()), 10000);
    this.updateSub = this.displayService.dataChanged$.subscribe(() => {
      this.loadServices();
      this.refresh();
    });
  }

  ngOnDestroy() {
    clearInterval(this.clock);
    clearInterval(this.resetTimer);
    this.updateSub?.unsubscribe();
  }

  private loadServices() {
    this.clientService.getServices().subscribe({
      next: (services) => this.services.set(services),
      error: () => this.error.set('Could not load services'),
    });
  }

  takeTicket(serviceId: number) {
    this.clientService.createTicket(serviceId).subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
        this.startCountdown();
      },
      error: (err) => this.error.set(typeof err.error === 'string' ? err.error : 'Could not create ticket'),
    });
  }

  // the ticket card closes by itself so the kiosk is ready for the next person
  private startCountdown() {
    clearInterval(this.resetTimer);
    this.countdown.set(RESET_SECONDS);
    this.resetTimer = setInterval(() => {
      this.countdown.update((s) => s - 1);
      if (this.countdown() <= 0) this.newTicket();
    }, 1000);
  }

  refresh() {
    const current = this.ticket();
    if (current) this.loadTicket(current.id);
  }

  newTicket() {
    clearInterval(this.resetTimer);
    this.ticket.set(null);
    this.error.set('');
  }

  private loadTicket(id: number) {
    this.clientService.getTicket(id).subscribe({
      next: (ticket) => this.ticket.set(ticket),
      error: () => this.ticket.set(null),
    });
  }

  logout() {
    this.accountService.logout();
  }
}
