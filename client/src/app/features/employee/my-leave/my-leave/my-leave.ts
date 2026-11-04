import { DatePicker } from '../../../../shared/date-picker/date-picker/date-picker';
import { localIso, rangeError } from '../../../../core/utils/date-range';
import { AlertModal } from '../../../../shared/alert-modal/alert-modal/alert-modal';
import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { LeaveService } from '../../../../core/services/leave-service';
import { Leave, LeaveBalance, LeaveTypeValue } from '../../../../types/reports';

const iso = localIso;

@Component({
  imports: [DatePipe, FormsModule, AlertModal, DatePicker],
  selector: 'app-my-leave',
  templateUrl: './my-leave.html',
})
export class MyLeave implements OnInit, OnDestroy {
  private leaveService = inject(LeaveService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;

  protected readonly reasons = [
    { value: LeaveTypeValue.Vacation, label: 'Vacation' },
    { value: LeaveTypeValue.Sick, label: 'Sick leave' },
  ];

  protected leaves = signal<Leave[]>([]);
  protected balance = signal<LeaveBalance | null>(null);
  protected error = signal('');
  protected success = signal('');

  protected type: number = LeaveTypeValue.Vacation;
  protected start = iso(new Date());
  protected end = iso(new Date());
  protected note = '';

  ngOnInit(): void {
    this.load();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.load());
  }

  ngOnDestroy(): void {
    this.updateSub?.unsubscribe();
  }

  load() {
    const year = new Date().getFullYear();
    this.leaveService.getMine(year).subscribe({
      next: (l) => this.leaves.set(l),
      error: () => this.error.set('Could not load your leave'),
    });
    this.leaveService.getBalance(year).subscribe({
      next: (b) => this.balance.set(b),
    });
  }

  submit() {
    this.success.set('');
    const err = rangeError(this.start, this.end);
    if (err) {
      this.error.set(err);
      return;
    }
    this.leaveService.requestLeave(this.type, this.start, this.end, this.note || null).subscribe({
      next: () => {
        this.error.set('');
        this.success.set('Request sent. Waiting for approval.');
        this.note = '';
        this.load();
      },
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not send request'),
    });
  }

  cancel(id: number) {
    this.leaveService.cancelMine(id).subscribe({
      next: () => this.load(),
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not cancel'),
    });
  }
}
