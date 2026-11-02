import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { ReportsService } from '../../../../core/services/reports-service';
import { EmployeeActivity, EmployeeReportRow, HoursReport, QueueReport } from '../../../../types/reports';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HoursMinutesPipe } from '../../../../core/pipes/hours-minutes-pipe';
const iso = (d: Date) => d.toISOString().slice(0, 10);
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return iso(d);
};


@Component({
  imports: [DatePipe, FormsModule, DecimalPipe, HoursMinutesPipe],
  selector: 'app-reports',
  styleUrl: './reports.css',
  templateUrl: './reports.html',
})
export class Reports implements OnInit, OnDestroy {
  private reportsService = inject(ReportsService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;
  protected tab = signal<'employees' | 'activity' | 'analytics'>('employees');
  protected from = daysAgo(30);
  protected to = iso(new Date());
  protected groupBy: 'day' | 'week' | 'month' | 'year' = 'week';
  protected analyticsEmployeeId: number | null = null;

  protected rows = signal<EmployeeReportRow[]>([]);
  protected selected = signal<EmployeeReportRow | null>(null);
  protected activity = signal<EmployeeActivity | null>(null);
  protected hours = signal<HoursReport | null>(null);
  protected queue = signal<QueueReport | null>(null);
  protected error = signal('');

  protected maxHours = computed(() => Math.max(1, ...(this.hours()?.points.map((p) => p.hours) ?? []))
  );
  protected sub = signal<'sessions' | 'tickets' | 'log'>('sessions');
  protected analyticsPage = signal(0);
  protected readonly analyticsPageSize = 8;
  protected pagedPoints = computed(() => {
    const pts = [...(this.hours()?.points ?? [])].reverse();
    const start = this.analyticsPage() * this.analyticsPageSize;
    return pts.slice(start, start + this.analyticsPageSize);
  });
  protected analyticsPages = computed(() =>
    Math.max(1, Math.ceil((this.hours()?.points.length ?? 0) / this.analyticsPageSize))
  );
  protected totalSessions = computed(() =>
    (this.hours()?.points ?? []).reduce((sum, p) => sum + p.sessions, 0)
  );
  protected page = signal(0);
  protected readonly pageSize = 8;

  private hmPipe = new HoursMinutesPipe();
  protected tabLabels: Record<'employees' | 'activity' | 'analytics', string> = {
    employees: 'Employees',
    activity: 'Activity',
    analytics: 'Analytics',
  };

  protected empPage = signal(0);
  protected pagedRows = computed(() => {
    const start = this.empPage() * this.pageSize;
    return this.rows().slice(start, start + this.pageSize);
  });
  protected empPages = computed(() => Math.max(1, Math.ceil(this.rows().length / this.pageSize)));

  protected totals = computed(() => {
    const r = this.rows();
    const sum = (f: (x: EmployeeReportRow) => number) => r.reduce((a, x) => a + f(x), 0);
    const served = sum((x) => x.ticketsServed);
    return {
      hours: sum((x) => x.hoursWorked),
      breaks: sum((x) => x.breakHours),
      sessions: sum((x) => x.sessions),
      served,
      noShows: sum((x) => x.noShows),
      avg: served ? sum((x) => x.avgServiceMinutes * x.ticketsServed) / served : 0,
    };
  });

  protected head = computed(() => {
    const e = this.selected();
    if (this.tab() === 'activity' && e) {
      return { initials: e.username.slice(0, 2).toUpperCase(), name: e.username,
        sub: e.counterName || 'No counter', hours: e.hoursWorked };
    }
    if (this.tab() === 'analytics') {
      const emp = this.rows().find((r) => r.id === this.analyticsEmployeeId);
      return { initials: emp ? emp.username.slice(0, 2).toUpperCase() : 'ALL',
        name: emp ? emp.username : 'All employees',
        sub: `Grouped by ${this.groupBy}`, hours: this.hours()?.totalHours ?? 0 };
    }
    return { initials: 'ALL', name: 'All employees',
      sub: `${this.rows().length} employees`, hours: this.totals().hours };
  });

  protected overview = computed<{ label: string; value: string; hint?: string }[]>(() => {
    const hm = (h: number) => this.hmPipe.transform(h);
    const min = (m: number) => `${Math.round(m * 10) / 10} min`;
    const e = this.selected();

    if (this.tab() === 'activity' && e) {
      return [
        { label: 'Hours worked', value: hm(e.hoursWorked), hint: 'Logged in, minus breaks' },
        { label: 'Breaks', value: hm(e.breakHours) },
        { label: 'Sessions', value: String(e.sessions) },
        { label: 'Served', value: String(e.ticketsServed), hint: 'Finished tickets' },
        { label: 'No-shows', value: String(e.noShows) },
        { label: 'Avg service', value: min(e.avgServiceMinutes), hint: 'Call to finish' },
      ];
    }
    if (this.tab() === 'analytics') {
      const h = this.hours();
      const q = this.queue();
      if (!h) return [];
      return [
        { label: 'Total hours', value: hm(h.totalHours), hint: 'Logged in, minus breaks' },
        { label: `Avg / ${this.groupBy}`, value: hm(h.avgHoursPerPeriod),
          hint: `Total ÷ ${h.points.length} ${this.groupBy}(s), empty ones included` },
        { label: 'Avg / worked day', value: hm(h.avgHoursPerWorkedDay),
          hint: `Total ÷ ${h.workedDays} days with a session` },
        { label: 'Avg session', value: hm(h.avgSessionHours),
          hint: `Total ÷ ${this.totalSessions()} sessions` },
        { label: 'Tickets served', value: String(h.totalTickets), hint: 'Finished only, no-shows excluded' },
        { label: 'Avg service time', value: min(h.avgServiceMinutes), hint: 'Call to finish' },
        ...(q ? [
          { label: 'Avg wait', value: min(q.avgWaitMinutes), hint: 'Ticket taken to called' },
          { label: 'Median wait', value: min(q.medianWaitMinutes), hint: 'Half of the clients wait less' },
          { label: 'Max wait', value: min(q.maxWaitMinutes), hint: 'Longest single wait' },
        ] : []),
      ];
    }
    const t = this.totals();
    return [
      { label: 'Total hours',value: hm(t.hours), hint: 'Logged in, minus breaks' },
      { label: 'Breaks', value: hm(t.breaks) },
      { label: 'Sessions', value: String(t.sessions) },
      { label: 'Served', value: String(t.served), hint: 'Finished tickets' },
      { label: 'No-shows', value: String(t.noShows) },
      { label: 'Avg service', value: min(t.avg), hint: 'Call to finish' },
    ];
  });

  ngOnInit(): void {
    this.apply();
    this.updateSub = this.displayService.dataChanged$.subscribe(() => this.refresh());
  }

  ngOnDestroy(): void {
    this.updateSub?.unsubscribe();
  }

  private refresh() {
    this.loadEmployees();
    this.loadHours(false);
    this.loadQueue();
    if (this.selected()) this.loadActivity(false);
  }

  setRange(days: number) {
    this.from = daysAgo(days);
    this.to = iso(new Date());
    this.apply()
  }
  apply() {
    this.loadEmployees();
    this.loadHours();
    this.loadQueue();
    if (this.selected()) this.loadActivity()
  }
  select(row: EmployeeReportRow) {
    this.selected.set(row);
    this.activity.set(null);
    this.page.set(0);
    this.tab.set('activity');
    this.loadActivity();
  }
  loadEmployees() {
    this.reportsService.getEmployees(this.from, this.to).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        const s=this.selected();
        if(s) this.selected.set(rows.find((r)=>r.id===s.id)??s);
        this.error.set('');
      },
      error: () => this.error.set('Could not load employees report'),
    });
  }
  loadActivity(reset = true) {
    const e = this.selected();
    if (!e) return;
    const range = { from: this.from, to: this.to };
    this.reportsService.getActivity(e.id, range.from, range.to).subscribe({
      next: (a) => {
        this.chartRange.set(range);
        if (reset) this.chartPage.set(0);
        this.activity.set(a);
      },
      error: () => this.error.set('Could not load activity'),
    });
  }
  loadHours(reset = true) {
    this.reportsService.getHours(this.from, this.to, this.groupBy, this.analyticsEmployeeId)
      .subscribe({
        next: (h) => {
          this.hours.set(h);
          if (reset) this.analyticsPage.set(0);
        },
        error: () => this.error.set('Could not load analytics'),
      })
  }
  loadQueue() {
    this.reportsService.getQueue(this.from, this.to).subscribe({
      next: (q) => this.queue.set(q),
      error: () => this.error.set('Could not load queue report'),
    });
  }

  protected hourBars = computed(() => {
    const all = this.queue()?.byHour ?? [];
    const used = all.filter((x) => x.tickets > 0);
    if (!used.length) return [];
    const first = used[0].hour;
    const last = used[used.length - 1].hour;
    const max = Math.max(...used.map((x) => x.tickets));
    return all
      .filter((x) => x.hour >= first && x.hour <= last)
      .map((x) => ({ ...x, label: String(x.hour).padStart(2, '0'), height: (x.tickets / max) * 100 }));
  });

  protected weekdayBars = computed(() => {
    const all = this.queue()?.byWeekday ?? [];
    const max = Math.max(1, ...all.map((x) => x.tickets));
    return all.map((x) => ({ ...x, height: (x.tickets / max) * 100 }));
  });

  protected chartPage=signal(0);
  private readonly chartDays=7;
  private chartRange=signal({from:this.from,to:this.to});

  protected chart=computed(()=>{
    const byDay=new Map<string,number>();
    for(const s of this.activity()?.sessions??[]){
      const day=s.loginAt.slice(0,10);
      byDay.set(day,(byDay.get(day)??0)+s.durationHours);
    }
    const all: {day:string;hours:number;label:string}[]=[];
    const {from,to}=this.chartRange();
    const last=new Date(to);
    for(const d=new Date(from); d<=last;d.setUTCDate(d.getUTCDate()+1)){
      const day=iso(d);
      all.push({day,hours:byDay.get(day)??0,label:`${day.slice(8,10)}.${day.slice(5,7)}`})
    }
    const max=Math.max(1,...all.map((b)=>b.hours));
    const end=all.length-this.chartPage()*this.chartDays;
  const start = Math.max(0, end - this.chartDays);
    const real = all
      .slice(start, end)
      .map((b) => ({ ...b, height: (b.hours / max) * 100, empty: false }));

    const pad = Array.from({ length: this.chartDays - real.length }, (_, i) => ({
      day: 'pad' + i, hours: 0, label: '', height: 0, empty: true,
    }));

    return {
      bars: [...pad, ...real],
      label: real.length ? `${real[0].label} - ${real[real.length - 1].label}` : '',
      canOlder: start > 0,
      canNewer: this.chartPage() > 0,
    };
  });

  protected ticketStats = computed(() => {
    const t = this.activity()?.tickets ?? [];
    const served = t.filter((x) => x.status === 'Finished').length;
    const noShow = t.filter((x) => x.status === 'NoShow').length;
    const total = served + noShow;
    const pct = total ? (served / total) * 100 : 0;
    const donut = total
      ? `conic-gradient(var(--color-success) 0 ${pct}%, var(--color-error) ${pct}% 100%)`
      : 'var(--color-base-300)';
    return { served, noShow, total, donut };
  });

  private slicePage<T>(items: T[]) {
    const start = this.page() * this.pageSize;
    return items.slice(start, start + this.pageSize);
  }
  protected pagedSessions = computed(() => this.slicePage(this.activity()?.sessions ?? []));
  protected pagedTickets = computed(() => this.slicePage(this.activity()?.tickets ?? []));
  protected pagedLog = computed(() => this.slicePage(this.activity()?.log ?? []));

  protected total = computed(() => {
    const a = this.activity();
    if (!a) return 0;
    return this.sub() === 'sessions' ? a.sessions.length
      : this.sub() === 'tickets' ? a.tickets.length
        : a.log.length;
  });
  protected pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  setSub(s: 'sessions' | 'tickets' | 'log') {
    this.sub.set(s);
    this.page.set(0);
  }
  setToday(){
    this.from=iso(new Date());
    this.to=iso(new Date())
    this.apply();
  }
  setWeek(){
    const back=(new Date().getDay()+6)%7;
    this.from=daysAgo(back);
    this.to=iso(new Date());
    this.apply();
  }
}