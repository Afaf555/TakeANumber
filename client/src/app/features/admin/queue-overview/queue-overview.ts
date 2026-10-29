import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { AdminService } from '../../../core/services/admin-service';
import { DisplayService } from '../../../core/services/display-service';
import { AdminTicket } from '../../../types/admin';
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
  protected waiting = computed(() => this.tickets().filter((t) => t.status === 'InQueue').length);

  ngOnInit() {
    this.load();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());
  }

  ngOnDestroy() {
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