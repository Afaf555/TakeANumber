import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ClientService } from '../../../core/services/client-service';
import { DisplayService } from '../../../core/services/display-service';
import { Service, Ticket } from '../../../types/tickets';

@Component({
  selector: 'app-take-ticket',
  imports: [],
  templateUrl: './take-ticket.html',
  styleUrl: './take-ticket.css',
})
export class TakeTicket implements OnInit, OnDestroy {
  private clientService = inject(ClientService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;

  protected services = signal<Service[]>([]);
  protected ticket = signal<Ticket | null>(null);
  protected error = signal('');
  private resetTimer?: ReturnType<typeof setTimeout>;

  ngOnInit() {
    this.loadServices();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => {
      this.loadServices();
      this.refresh();
    });
  }

  private loadServices() {
    this.clientService.getServices().subscribe({
      next: (services) => this.services.set(services),
      error: () => this.error.set('Could not load services'),
    });
  }
  ngOnDestroy() {
    clearTimeout(this.resetTimer);
    this.updateSub?.unsubscribe();
    // this.displayService.stopConnection();
  }

  takeTicket(serviceId: number) {
    this.clientService.createTicket(serviceId).subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
   },
      error: (err) =>
        this.error.set(typeof err.error === 'string' ? err.error : 'Could not create ticket'),
    });
  }

  refresh() {
    const current = this.ticket();
    if (current) this.loadTicket(current.id);
  }

  newTicket() {
    this.ticket.set(null);
    this.error.set('');
  }

  private loadTicket(id: number) {
    this.clientService.getTicket(id).subscribe({
      next: (ticket) => this.ticket.set(ticket),
      error: () => this.ticket.set(null),
    });
  }

}