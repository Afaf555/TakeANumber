import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CalendarService } from '../../../../core/services/calendar-service';
import { EmployeeWork, NonWorkingDay, NonWorkingDayWork } from '../../../../types/calendar';

type DayCell = {
  day: number;
  key: string;
  weekend: boolean;
  holiday: NonWorkingDay | undefined;
  workers: EmployeeWork[];
  today: boolean;
};

@Component({
  selector: 'app-calendar',
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './calendar.html',
  styleUrl: './calendar.css',
})
export class Calendar implements OnInit {
  private calendarService = inject(CalendarService);

  protected days = signal<NonWorkingDay[]>([]);
  protected work = signal<NonWorkingDayWork[]>([]);
  protected error = signal('');
  protected message = signal('');

  protected year = signal(new Date().getFullYear());
  protected month = signal(new Date().getMonth());
  protected newDate = '';
  protected newName = '';
  protected from = `${new Date().getFullYear()}-01-01`;
  protected to = new Date().toISOString().slice(0, 10);
  protected weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  protected monthStart = computed(() => new Date(this.year(), this.month(), 1));

  protected cells = computed<(DayCell | null)[]>(() => {
    const y = this.year();
    const m = this.month();
    const pad = (n: number) => String(n).padStart(2, '0');
    const offset = (new Date(y, m, 1).getDay() + 6) % 7; // Monday first
    const count = new Date(y, m + 1, 0).getDate();
    const holidays = new Map(this.days().map((d) => [d.date.slice(0, 10), d]));
    const worked = new Map(this.work().map((w) => [w.date.slice(0, 10), w.employees]));
    const todayKey = new Date().toISOString().slice(0, 10);

    const result: (DayCell | null)[] = Array(offset).fill(null);
    for (let d = 1; d <= count; d++) {
      const key = `${y}-${pad(m + 1)}-${pad(d)}`;
      const dow = new Date(y, m, d).getDay();
      result.push({
        day: d,
        key,
        weekend: dow === 0 || dow === 6,
        holiday: holidays.get(key),
        workers: worked.get(key) ?? [],
        today: key === todayKey,
      });
    }
    return result;
  });

  protected workedDays = computed(() => this.work().filter((d) => d.employees.length > 0));

  protected summary = computed(() => {
    const map = new Map<number, { username: string; days: number; hours: number; tickets: number }>();
    for (const day of this.work()) {
      for (const e of day.employees) {
        const s = map.get(e.employeeId) ?? { username: e.username, days: 0, hours: 0, tickets: 0 };
        s.days++;
        s.hours += e.hours;
        s.tickets += e.ticketsServed;
        map.set(e.employeeId, s);
      }
    }
    return [...map.values()].sort((a, b) => a.username.localeCompare(b.username));
  });

  ngOnInit() {
    this.loadDays();
    this.loadWork();
  }

  setYear(y: number) {
    this.year.set(y);
    this.loadDays();
  }

  prevMonth() {
    if (this.month() === 0) {
      this.month.set(11);
      this.setYear(this.year() - 1);
    } else this.month.update((m) => m - 1);
  }

  nextMonth() {
    if (this.month() === 11) {
      this.month.set(0);
      this.setYear(this.year() + 1);
    } else this.month.update((m) => m + 1);
  }

  loadDays() {
    this.calendarService.getDays(this.year()).subscribe({
      next: (days) => this.days.set(days),
      error: () => this.error.set('Could not load calendar'),
    });
  }

  loadWork() {
    this.calendarService.getWork(this.from, this.to).subscribe({
      next: (work) => this.work.set(work),
      error: () => this.error.set('Could not load report'),
    });
  }

  add() {
    if (!this.newDate || !this.newName.trim()) {
      this.error.set('Pick a date and enter a name');
      return;
    }
    this.calendarService.addDay(this.newDate, this.newName).subscribe({
      next: () => {
        this.newDate = '';
        this.newName = '';
        this.error.set('');
        this.message.set('');
        this.loadDays();
        this.loadWork();
      },
      error: (e) => this.error.set(typeof e.error === 'string' ? e.error : 'Could not add day'),
    });
  }

  remove(id: number) {
    if (!confirm('Remove this day from the calendar?')) return;
    this.calendarService.removeDay(id).subscribe({
      next: () => {
        this.message.set('');
        this.loadDays();
        this.loadWork();
      },
      error: () => this.error.set('Could not remove day'),
    });
  }

  importYear() {
    this.calendarService.importYear(this.year()).subscribe({
      next: (added) => {
        this.message.set(`Imported ${added} days for ${this.year()}`);
        this.error.set('');
        this.loadDays();
        this.loadWork();
      },
      error: (e) => {
        this.message.set('');
        this.error.set(typeof e.error === 'string' ? e.error : 'Import failed');
      },
    });
  }
}