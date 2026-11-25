import { PageHeader } from '../../../../shared/page-header/page-header';
import { DatePicker } from '../../../../shared/date-picker/date-picker/date-picker';
import { localIso, rangeError } from '../../../../core/utils/date-range';
import { downloadCsv, fmtDate } from '../../../../core/utils/csv';
import { AlertModal } from '../../../../shared/alert-modal/alert-modal/alert-modal';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe, Location } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { LeaveService } from '../../../../core/services/leave-service';
import { ReportsService } from '../../../../core/services/reports-service';
import { HoursMinutesPipe } from '../../../../core/pipes/hours-minutes-pipe';
import { EmployeePhases, Leave, LeaveTypeValue, PhaseName, PhaseSegment, PhaseTimeline, PhaseTimelineDay, PtoRow, WorkforceReport } from '../../../../types/reports';
import { SettingsService } from '../../../../core/services/settings-service';
import { DEFAULT_SETTINGS, shiftTime, WorkSettings } from '../../../../types/settings';

const iso = localIso;

type PhaseKey = 'servingMinutes' | 'idleMinutes' | 'neglectMinutes' | 'breakMinutes' | 'afterHoursMinutes';
type PhaseMetric = { key: PhaseKey; label: string; color: string };
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return iso(d);
};

type WorkforceTab = 'attendance' | 'phases' | 'pto';

@Component({
  imports: [PageHeader, DatePipe, DecimalPipe, FormsModule, HoursMinutesPipe, AlertModal, DatePicker],
  selector: 'app-workforce',
  styleUrl: './workforce.css',
  templateUrl: './workforce.html',
})
export class Workforce implements OnInit, OnDestroy {
  private reportsService = inject(ReportsService);
  private leaveService = inject(LeaveService);
  private displayService = inject(DisplayService);
  private settingsService = inject(SettingsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private location = inject(Location);
  private updateSub?: Subscription;

  protected rules = signal<WorkSettings>({ ...DEFAULT_SETTINGS });
  protected lateAfter = computed(() => shiftTime(this.rules().openTime, this.rules().lateGraceMinutes));
  protected earlyBefore = computed(() => shiftTime(this.rules().closeTime, -this.rules().earlyGraceMinutes));

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
  protected tab = signal<WorkforceTab>('attendance');
  protected phases = signal<EmployeePhases[]>([]);

  protected readonly phaseMetrics: PhaseMetric[] = [
    { key: 'servingMinutes', label: 'Serving', color: 'bg-success' },
    { key: 'idleMinutes', label: 'Idle', color: 'bg-info' },
    { key: 'neglectMinutes', label: 'Neglect', color: 'bg-error' },
    { key: 'breakMinutes', label: 'Break', color: 'bg-warning' },
    { key: 'afterHoursMinutes', label: 'After hours', color: 'bg-neutral' },
  ];
  protected phaseMetric = signal<PhaseMetric>(this.phaseMetrics[2]);
  protected selectedPhase = signal<EmployeePhases | null>(null);
  protected phaseEmployeeId = signal<number | null>(null);

  protected filteredPhases = computed(() => {
    const id = this.phaseEmployeeId();
    return id == null ? this.phases() : this.phases().filter((p) => p.employeeId === id);
  });

  protected setPhaseEmployee(id: number | null) {
    this.phaseEmployeeId.set(id);
    this.selectedPhase.set(null);
    this.selectedDay.set(null);
    this.writeUrl();
    this.loadTimeline();
  }

  protected timeline = signal<PhaseTimeline | null>(null);
  protected selectedDay = signal<PhaseTimelineDay | null>(null);
  protected readonly phaseColors: Record<PhaseName, string> = {
    Session: 'bg-base-300',
    Serving: 'bg-success',
    Idle: 'bg-info',
    Neglect: 'bg-error',
    Break: 'bg-warning',
  };

  protected timelineAxis = computed(() => {
    const tl = this.timeline();
    if (!tl || !tl.days.length) return null;
    const all = tl.days.flatMap((d) => d.sessions);
    const start = Math.floor(Math.min(tl.openMinute, ...all.map((s) => s.startMinute)) / 60) * 60;
    const end = Math.ceil(Math.max(tl.closeMinute, ...all.map((s) => s.endMinute)) / 60) * 60;
    const hours = Array.from({ length: (end - start) / 60 + 1 }, (_, i) => start / 60 + i);
    return { start, end, hours };
  });

  protected readonly slotOptions = [15, 30, 60];
  protected readonly legendPhases: PhaseName[] = ['Serving', 'Idle', 'Neglect', 'Break'];
  protected slotMinutes = signal(15);
  protected hoverCell = signal<{ day: string; time: string; parts: { phase: PhaseName; minutes: number }[] } | null>(null);
  private readonly phaseOrder: PhaseName[] = ['Serving', 'Neglect', 'Break', 'Idle'];

  private hm(m: number) {
    const total = Math.round(m);
    return `${String(Math.floor(total / 60) % 24).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`;
  }

  private overlap(a: number, b: number, s: { startMinute: number; endMinute: number }) {
    return Math.max(0, Math.min(b, s.endMinute) - Math.max(a, s.startMinute));
  }

  protected timelineGrid = computed(() => {
    const tl = this.timeline();
    const ax = this.timelineAxis();
    if (!tl || !ax) return null;
    const slot = this.slotMinutes();
    const columns = Math.ceil((ax.end - ax.start) / slot);
    const starts = Array.from({ length: columns }, (_, i) => ax.start + i * slot);

    const header = starts
      .filter((m) => m % 60 === 0)
      .map((m) => ({
        label: this.hm(m).slice(0, 2),
        col: (m - ax.start) / slot + 2,
        span: Math.max(1, 60 / slot),
        work: m >= tl.openMinute && m < tl.closeMinute,
      }));

    const rows = tl.days.map((d) => {
      const cells = starts.map((a) => {
        const b = a + slot;
        const parts = this.phaseOrder
          .map((phase) => ({
            phase,
            minutes: d.segments.filter((s) => s.phase === phase).reduce((sum, s) => sum + this.overlap(a, b, s), 0),
          }))
          .filter((p) => p.minutes > 0.5);
        const used = parts.reduce((sum, p) => sum + p.minutes, 0);
        const main = parts.reduce<{ phase: PhaseName; minutes: number } | null>(
          (best, p) => (!best || p.minutes > best.minutes ? p : best), null);
        return {
          start: a,
          time: `${this.hm(a)} - ${this.hm(b)}`,
          parts,
          color: main ? this.phaseColors[main.phase] : '',
          opacity: main ? 0.35 + 0.65 * Math.min(1, used / slot) : 1,
          work: a >= tl.openMinute && a < tl.closeMinute,
        };
      });
      const logged = d.sessions.reduce((sum, s) => sum + (s.endMinute - s.startMinute), 0);
      return {
        day: d,
        cells,
        logged,
        from: d.sessions[0]?.from ?? '',
        to: d.sessions[d.sessions.length - 1]?.to ?? '',
      };
    });

    return { columns, header, rows, template: `7.5rem repeat(${columns}, minmax(8px, 1fr)) 4.5rem` };
  });

  protected dayTotals(d: PhaseTimelineDay) {
    return this.phaseOrder
      .map((phase) => ({
        phase,
        minutes: d.segments.filter((s) => s.phase === phase).reduce((sum, s) => sum + s.endMinute - s.startMinute, 0),
      }))
      .filter((p) => p.minutes > 0);
  }

 protected visibleSegments(d: PhaseTimelineDay) {
  return d.segments.filter((s) => s.phase === 'Serving' || s.endMinute - s.startMinute >= 0.5);
}

  protected mins(m: number) {
    const r = Math.round(m);
    return r >= 60 ? `${Math.floor(r / 60)}h ${r % 60}m` : `${r}m`;
  }

  protected phaseHeatTemplate(days: number) {
    return `7.5rem repeat(${days}, minmax(14px, 1fr)) 4.5rem`;
  }

  protected pos(minute: number) {
    const a = this.timelineAxis();
    return a ? ((minute - a.start) / (a.end - a.start)) * 100 : 0;
  }

  protected segTitle(s: PhaseSegment) {
    const name = s.phase === 'Session' ? 'Logged in' : s.phase;
    return `${name}: ${s.from} - ${s.to} (${this.duration(s)})` + (s.ticket ? ` · ${s.ticket}` : '');
  }

  protected duration(s: PhaseSegment) {
    const m = Math.round(s.endMinute - s.startMinute);
    if(m===0) return '<1m';
    return m >= 60 ? `${Math.floor(m / 60)}h ${m % 60}m` : `${m}m`;
  }

  protected loadTimeline() {
    const id = this.phaseEmployeeId();
    if (id == null) {
      this.timeline.set(null);
      return;
    }
    this.reportsService.getPhaseTimeline(this.from, this.to, id).subscribe({
      next: (tl) => {
        this.timeline.set(tl);
        const day = this.pendingDay ?? this.selectedDay()?.date.slice(0, 10);
        this.pendingDay = null;
        this.selectedDay.set(day ? tl.days.find((d) => d.date.slice(0, 10) === day) ?? null : null);
      },
      error: () => this.error.set('Could not load timeline'),
    });
  }

  protected phaseHeat = computed(() => {
    const rows = this.filteredPhases();
    if (!rows.length) return null;
    const metric = this.phaseMetric();

    const dayKeys = [...new Set(rows.map((p) => p.date.slice(0, 10)))].sort();
    const days = this.daysBetween(dayKeys[0], dayKeys[dayKeys.length - 1]);

    const byKey = new Map(rows.map((p) => [p.employeeId + '|' + p.date.slice(0, 10), p]));
    const max = Math.max(1, ...rows.map((p) => p[metric.key]));

    const employees = [...new Map(rows.map((p) => [p.employeeId, p.username])).entries()]
      .sort((a, b) => a[1].localeCompare(b[1]));

    return {
      max,
      days: days.map((d) => ({
        key: d,
        label: d.slice(8, 10),
        month: d.slice(8, 10) === '01' || d === days[0] ? d.slice(5, 7) + '/' + d.slice(2, 4) : '',
        weekend: [0, 6].includes(new Date(d + 'T00:00:00').getDay()),
      })),
      rows: employees.map(([id, username]) => {
        const cells = days.map((d) => {
          const p = byKey.get(id + '|' + d) ?? null;
          const value = p ? p[metric.key] : 0;
          return { day: d, p, value, opacity: value ? 0.15 + (value / max) * 0.85 : 0 };
        });
        return { id, username, cells, total: cells.reduce((s, c) => s + c.value, 0) };
      }),
    };
  });

  private daysBetween(from: string, to: string): string[] {
    const result: string[] = [];
    const d = new Date(from + 'T00:00:00');
    const end = new Date(to + 'T00:00:00');
    while (d <= end) {
      result.push(iso(d));
      d.setDate(d.getDate() + 1);
    }
    return result;
  }

  protected cellTitle(username: string, day: string, p: EmployeePhases | null) {
    const date = day.split('-').reverse().join('.');
    if (!p) return `${username} · ${date} · no session`;
    const m = (x: number) => `${Math.floor(x / 60)}h ${Math.round(x % 60)}m`;
    return `${username} · ${date}\nServing: ${m(p.servingMinutes)}\nIdle: ${m(p.idleMinutes)}\n`
      + `Neglect: ${m(p.neglectMinutes)}\nBreak: ${m(p.breakMinutes)}\nAfter hours: ${m(p.afterHoursMinutes)}`;
  }

  private pendingCell: string | null = null;
  private pendingDay: string | null = null;

  private readUrl() {
    const q = this.route.snapshot.queryParamMap;

    const tab = q.get('tab');
    if (tab === 'phases' || tab === 'pto') this.tab.set(tab);

    const range = Number(q.get('range'));
    const from = q.get('from');
    const to = q.get('to');
    if ([7, 30, 90].includes(range)) {
      this.from = daysAgo(range);
      this.to = iso(new Date());
    } else if (from && to && !rangeError(from, to)) {
      this.from = this.appliedFrom = from;
      this.to = this.appliedTo = to;
      this.lastClick = 'apply';
    }

    const emp = Number(q.get('emp'));
    if (emp) this.phaseEmployeeId.set(emp);

    const metric = this.phaseMetrics.find((m) => m.key === q.get('metric'));
    if (metric) this.phaseMetric.set(metric);

    const slot = Number(q.get('slot'));
    if (this.slotOptions.includes(slot)) this.slotMinutes.set(slot);

    const year = Number(q.get('year'));
    if (year) this.year = year;

    this.pendingCell = q.get('cell');
    this.pendingDay = q.get('day');
  }

  private writeUrl() {
    const custom = this.lastClick === 'apply';
    const sel = this.selectedPhase();
    const day = this.selectedDay();
  const url = this.router.createUrlTree([], {
  relativeTo: this.route,
  queryParams: {
        tab: this.tab() === 'attendance' ? null : this.tab(),
        range: custom ? null : this.activeRange(),
        from: custom ? this.from : null,
        to: custom ? this.to : null,
        emp: this.phaseEmployeeId(),
        metric: this.phaseMetric() === this.phaseMetrics[2] ? null : this.phaseMetric().key,
        slot: this.slotMinutes() === 15 ? null : this.slotMinutes(),
        year: this.year === new Date().getFullYear() ? null : this.year,
        cell: sel ? `${sel.employeeId}_${sel.date.slice(0, 10)}` : null,
        day: day ? day.date.slice(0, 10) : null,
      },
    });
    this.location.replaceState(url.toString());
  }

  protected setTab(t: WorkforceTab) {
    this.tab.set(t);
    this.writeUrl();
  }

  protected setMetric(m: PhaseMetric) {
    this.phaseMetric.set(m);
    this.writeUrl();
  }

  protected setSlot(minutes: number) {
    this.slotMinutes.set(minutes);
    this.writeUrl();
  }

  protected selectPhase(p: EmployeePhases | null) {
    this.selectedPhase.set(p);
    this.writeUrl();
  }

  protected selectDay(d: PhaseTimelineDay | null) {
    this.selectedDay.set(d);
    this.writeUrl();
  }

  protected yearChanged() {
    this.loadPto();
    this.loadLeaves();
    this.writeUrl();
  }

  ngOnInit(): void {
    this.readUrl();
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
    this.writeUrl();
    this.loadReport();
  }

  setRange(days: number) {
    this.lastClick = 'range';
    this.from = daysAgo(days);
    this.to = iso(new Date());
    this.writeUrl();
    this.loadReport();
  }
    protected phaseTotal(p: EmployeePhases) {
    return p.servingMinutes + p.idleMinutes + p.neglectMinutes + p.breakMinutes;
  }

  protected phasePct(value: number, p: EmployeePhases) {
    const total = this.phaseTotal(p);
    return total ? (value / total) * 100 : 0;
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
        this.reportsService.getPhases(this.from, this.to).subscribe({
      next: (rows) => {
        this.phases.set(rows);
        const sel = this.selectedPhase();
        const cell = this.pendingCell ?? (sel ? `${sel.employeeId}_${sel.date.slice(0, 10)}` : null);
        this.pendingCell = null;
        this.selectedPhase.set(cell ? rows.find((p) => `${p.employeeId}_${p.date.slice(0, 10)}` === cell) ?? null : null);
      },
      error: () => this.error.set('Could not load phases'),
    });
    this.loadTimeline();
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

  
  exportPhases() {
    const id = this.phaseEmployeeId();
    const who = id == null ? '' : '_' + (this.employees().find((e) => e.id === id)?.username ?? id);
    downloadCsv(
      `phases_${this.from}_${this.to}${who}.csv`,
      ['Date', 'Employee', 'Serving (min)', 'Idle (min)', 'Neglect (min)', 'Break (min)', 'After hours (min)'],
      [...this.filteredPhases()]
        .sort((a, b) => a.date.localeCompare(b.date) || a.username.localeCompare(b.username))
        .map((p) => [
          fmtDate(p.date), p.username, Math.round(p.servingMinutes), Math.round(p.idleMinutes),
          Math.round(p.neglectMinutes), Math.round(p.breakMinutes), Math.round(p.afterHoursMinutes),
        ]),
    );
  }


  exportTimeline() {
    const tl = this.timeline();
    const id = this.phaseEmployeeId();
    if (!tl || id == null) return;
    const name = this.employees().find((e) => e.id === id)?.username ?? String(id);
    const rows = tl.days.flatMap((d) => [
      ...d.sessions.map((s) => ({ d, s, phase: 'Logged in' })),
      ...this.visibleSegments(d).map((s) => ({ d, s, phase: s.phase as string })),
    ].sort((a, b) => a.s.startMinute - b.s.startMinute || (a.phase === 'Logged in' ? -1 : 1))
      .map((r) => [fmtDate(r.d.date), r.s.from, r.s.to, r.phase, Math.round(r.s.endMinute - r.s.startMinute), r.s.ticket ?? '']));
    downloadCsv(
      `timeline_${name}_${this.from}_${this.to}.csv`,
      ['Date', 'From', 'To', 'Phase', 'Minutes', 'Ticket'],
      rows,
    );
  }
}