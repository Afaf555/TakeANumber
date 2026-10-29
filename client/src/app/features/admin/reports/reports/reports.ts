import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../../core/services/display-service';
import { ReportsService } from '../../../../core/services/reports-service';
import { EmployeeActivity, EmployeeReportRow, HoursReport } from '../../../../types/reports';
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