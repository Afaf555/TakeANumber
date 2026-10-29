import { Component, inject, OnInit, signal } from '@angular/core';
import { EmployeeService } from '../../../core/services/employee-service';
import { EmployeeTicket } from '../../../types/employee';

@Component({
  selector: 'app-counter-panel',
  imports: [],
  templateUrl: './counter-panel.html',
  styleUrl: './counter-panel.css',
})
export class CounterPanel implements OnInit {
  private employeeService = inject(EmployeeService);

  protected ticket = signal<EmployeeTicket | null>(null);
  protected error = signal('');
  protected onBreak=signal(false);

  ngOnInit() {
    this.employeeService.getCurrent().subscribe({
      next: (ticket) => this.ticket.set(ticket),
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