import { DatePicker } from '../../../../shared/date-picker/date-picker/date-picker';
import { localIso, rangeError } from '../../../../core/utils/date-range';
import { downloadCsv } from '../../../../core/utils/csv';
import { AlertModal } from '../../../../shared/alert-modal/alert-modal/alert-modal';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { LeaveService } from '../../../../core/services/leave-service';
import { ReportsService } from '../../../../core/services/reports-service';
import { HoursMinutesPipe } from '../../../../core/pipes/hours-minutes-pipe';
import { Leave, LeaveTypeValue, PtoRow, WorkforceReport } from '../../../../types/reports';
import { SettingsService } from '../../../../core/services/settings-service';
import { DEFAULT_SETTINGS, shiftTime, WorkSettings } from '../../../../types/settings';

const iso = localIso;
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return iso(d);
};

@Component({
  imports: [DatePipe, DecimalPipe, FormsModule, HoursMinutesPipe, AlertModal, DatePicker],
  selector: 'app-workforce',
  styleUrl: './workforce.css',
  templateUrl: './workforce.html',
})
export class Workforce implements OnInit, OnDestroy {
  private reportsService = inject(ReportsService);
  private leaveService = inject(LeaveService);
  private displayService = inject(DisplayService);
  private settingsService = inject(SettingsService);
  private updateSub?: Subscription;

  protected rules = signal<WorkSettings>({ ...DEFAULT_SETTINGS });
  protected lateAfter = computed(() => shiftTime(this.rules().openTime, this.rules().lateGraceMinutes));
  protected earlyBefore = computed(() => shiftTime(this.rules().closeTime, -this.rules().earlyGraceMinutes));

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
  private lastClick: 'range' | 'apply' = 'range';
  private appliedFrom = '';
  private appliedTo = '';

  ngOnInit(): void {
    this.settingsService.get().subscribe({ next: (s) => this.rules.set(s) });
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
  protected activeRange(): number | null {
    if (this.to !== iso(new Date())) return null;
    return [7, 30, 90].find((days) => this.from === daysAgo(days)) ?? null;
  }
  protected rangeActive(days: number) {
    return this.lastClick === 'range' && this.activeRange() === days;
  }

  protected applyActive() {
    return this.lastClick === 'apply' && this.from === this.appliedFrom && this.to === this.appliedTo;
  }

  apply() {
    this.lastClick = 'apply';
    this.appliedFrom = this.from;
    this.appliedTo = this.to;
    this.loadReport();
  }

  setRange(days: number) {
    this.lastClick = 'range';
    this.from = daysAgo(days);
    this.to = iso(new Date());
    this.loadReport();
  }
  loadReport() {
    const err = rangeError(this.from, this.to);
    if (err) {
      this.error.set(err);
      return;
    }
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
    if (this.leaveEmployeeId == null) {
      this.error.set('Please choose an employee');
      return;
    }
    const leaveErr = rangeError(this.leaveStart, this.leaveEnd);
    if (leaveErr) {
      this.error.set(leaveErr);
      return;
    }
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

  exportAttendance() {
    downloadCsv(
      `attendance_${this.from}_${this.to}.csv`,
      ['Employee', 'Tenure (days)', 'Available days', 'Worked', 'Absent', 'Absence %', 'Vacation', 'Sick',
        'Late', 'Avg late (min)', 'Early out', 'Overtime (h)'],
      this.employees().map((e) => [
        e.username, e.tenureDays, e.expectedDays, e.workedDays, e.absentDays, e.absenteeismRate,
        e.vacationDays, e.sickDays, e.lateArrivals, e.avgLateMinutes, e.earlyDepartures, e.overtimeHours,
      ]),
    );
  }

  exportPto() {
    downloadCsv(
      `paid_leave_${this.year}.csv`,
      ['Employee', 'Vacation allowance', 'Vacation used', 'Vacation scheduled', 'Vacation remaining',
        'Sick allowance', 'Sick used', 'Sick remaining'],
      this.pto().map((p) => [
        p.username, p.vacationAllowance, p.vacationUsed, p.vacationScheduled, p.vacationRemaining,
        p.sickAllowance, p.sickUsed, p.sickRemaining,
      ]),
    );
  }
}
