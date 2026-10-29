import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../core/services/display-service';
import { EmployeeService } from '../../../core/services/employee-service';
import { EmployeeTicket } from '../../../types/employee';

@Component({
  selector: 'app-counter-panel',
  imports: [],
  templateUrl: './counter-panel.html',
  styleUrl: './counter-panel.css',
})
export class CounterPanel implements OnInit, OnDestroy {
  private employeeService = inject(EmployeeService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;

  protected ticket = signal<EmployeeTicket | null>(null);
  protected error = signal('');
  protected onBreak=signal(false);

  ngOnInit() {
    this.load();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());
  }

  ngOnDestroy() {
    this.updateSub?.unsubscribe();
  }

  private load() {
    this.employeeService.getCurrent().subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not load current ticket'),
    });
    this.employeeService.getStatus().subscribe({
      next:(s)=>this.onBreak.set(s.isOnBreak),
    });
  }
  startBreak(){
    this.employeeService.startBreak().subscribe({
      next:()=>{this.onBreak.set(true);this.error.set('');},
      error:(err)=>this.showError(err,'Could not start break'),
    })
  }

  endBreak(){
    this.employeeService.endBreak().subscribe({
      next:()=>{this.onBreak.set(false);this.error.set('');},
      error:(err)=>this.showError(err,'Could not end break'),
    });
  }

  callNext() {
    this.employeeService.callNext().subscribe({
      next: (ticket) => {
        this.ticket.set(ticket);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not call next ticket'),
    });
  }

  finish() {
    this.employeeService.finish().subscribe({
      next: () => {
        this.ticket.set(null);
        this.error.set('');
      },
      error: (err) => this.showError(err, 'Could not finish ticket'),
    });
  }

  private showError(err: any, fallback: string) {
    this.error.set(typeof err.error === 'string' ? err.error : fallback);
  }
  noShow(){
    this.employeeService.noShow().subscribe({
      next:()=>{
        this.ticket.set(null);
        this.error.set('');
      },
      error:(err)=>this.showError(err,'Could not mark ticket as no-show'),
    });
  }
}