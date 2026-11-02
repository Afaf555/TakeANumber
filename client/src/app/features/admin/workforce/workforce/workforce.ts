import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { LeaveService } from '../../../../core/services/leave-service';
import { ReportsService } from '../../../../core/services/reports-service';
import { HoursMinutesPipe } from '../../../../core/pipes/hours-minutes-pipe';
import { Leave, LeaveTypeValue, PtoRow, WorkforceReport } from '../../../../types/reports';

const iso = (d: Date) => d.toISOString().slice(0, 10);
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return iso(d);
};

@Component({
  imports: [DatePipe, DecimalPipe, FormsModule, HoursMinutesPipe],
  selector: 'app-workforce',
  styleUrl: './workforce.css',
  templateUrl: './workforce.html',
})
export class Workforce implements OnInit, OnDestroy {
  private reportsService = inject(ReportsService);
  private leaveService = inject(LeaveService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;

  protected tab = signal<'attendance' | 'pto'>('attendance');
  protected from = daysAgo(30);
  protected to = iso(new Date());
  protected year = new Date().getFullYear();

  protected report = signal<WorkforceReport | null>(null);
  protected pto = signal<PtoRow[]>([]);
  protected leaves = signal<Leave[]>([]);
  protected error = signal('');

  protected leaveEmployeeId: number | null = null;
  protected leaveType: number = LeaveTypeValue.Vacation;
  protected leaveStart = iso(new Date());
  protected leaveEnd = iso(new Date());
  protected leaveNote = '';

  protected readonly vacation = LeaveTypeValue.Vacation;
  protected readonly sick = LeaveTypeValue.Sick;

  protected pending = computed(() => this.leaves().filter((l) => l.status === 'Pending'));
  protected employees = computed(() => this.report()?.employees ?? []);
  protected maxCounterOvertime = computed(() =>
    Math.max(1, ...(this.report()?.overtimeByCounter.map((c) => c.overtimeHours) ?? []))
  );

  ngOnInit(): void {
    this.loadAll();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.loadAll());
  }

  ngOnDestroy(): void {
    this.updateSub?.unsubscribe();
  }

  loadAll() {
    this.loadReport();
    this.loadPto();
    this.loadLeaves();
  }

  setRange(days: number) {
    this.from = daysAgo(days);
    this.to = iso(new Date());
    this.loadReport();
  }

  loadReport() {
    this.reportsService.getWorkforce(this.from, this.to).subscribe({
      next: (r) => {
        this.report.set(r);
        this.error.set('');
      },
      error: () => this.error.set('Could not load workforce report'),
    });
  }

  loadPto() {
    this.reportsService.getPto(this.year).subscribe({
      next: (rows) => this.pto.set(rows),
      error: () => this.error.set('Could not load PTO balances'),
    });
  }

  loadLeaves() {
    this.leaveService.getLeaves(this.year).subscribe({
      next: (list) => this.leaves.set(list),
      error: () => this.error.set('Could not load leaves'),
    });
  }

  addLeave() {
    if (this.leaveEmployeeId == null) return;
    this.leaveService
      .createLeave(this.leaveEmployeeId, this.leaveType, this.leaveStart, this.leaveEnd, this.leaveNote || null)
      .subscribe({
        next: () => {
          this.error.set('');
          this.leaveNote = '';
          this.loadAll();
        },
        error: (e) =>
          this.error.set(typeof e.error === 'string' ? e.error : 'Could not add leave'),
      });
  }

  approve(id: number) {
    this.leaveService.approve(id).subscribe({
      next: () => this.loadAll(),
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not approve'),
    });
  }

  reject(id: number) {
    this.leaveService.reject(id).subscribe({
      next: () => this.loadAll(),
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not reject'),
    });
  }

  removeLeave(id: number) {
    this.leaveService.deleteLeave(id).subscribe({
      next: () => this.loadAll(),
      error: () => this.error.set('Could not remove leave'),
    });
  }
}