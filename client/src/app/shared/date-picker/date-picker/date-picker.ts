import { Component, ElementRef, HostListener, computed, inject, input, model, signal } from '@angular/core';

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];
const pad = (n: number) => String(n).padStart(2, '0');
const toIso = (y: number, m: number, d: number) => `${y}-${pad(m + 1)}-${pad(d)}`;
const parse = (s: string) => {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(s ?? '');
  return m ? { y: +m[1], m: +m[2] - 1, d: +m[3] } : null;
};
const todayIso = () => {
  const n = new Date();
  return toIso(n.getFullYear(), n.getMonth(), n.getDate());
};

interface Cell {
  iso: string;
  day: number;
  inMonth: boolean;
  disabled: boolean;
  today: boolean;
  selected: boolean;
  weekend: boolean;
}

@Component({
  selector: 'app-date-picker',
  host: { class: 'relative inline-block' },
  template: `
    <button type="button" [attr.id]="inputId() || null"
      class="input input-sm w-full rounded-full flex items-center justify-between gap-2 cursor-pointer"
      aria-haspopup="dialog" [attr.aria-expanded]="open()" (click)="toggle()">
      <span [class.opacity-50]="!value()">{{ label() || placeholder() }}</span>
      <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 opacity-60" fill="none" viewBox="0 0 24 24"
        stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round"
          d="M8 7V3m8 4V3M4 11h16M5 5h14a1 1 0 011 1v14a1 1 0 01-1 1H5a1 1 0 01-1-1V6a1 1 0 011-1z" />
      </svg>
    </button>

    @if (open()) {
    <div role="dialog" aria-label="Choose date"
      class="absolute z-50 mt-1 w-72 rounded-box border border-base-300 bg-base-100 p-3 text-sm font-normal normal-case text-base-content shadow-lg"
      [class.right-0]="alignRight()" [class.left-0]="!alignRight()">
      <div class="mb-2 flex items-center justify-between">
        <div class="flex gap-0.5">
          <button type="button" class="btn btn-ghost btn-xs btn-circle" aria-label="Previous year" (click)="shiftYear(-1)">«</button>
          <button type="button" class="btn btn-ghost btn-xs btn-circle" aria-label="Previous month" (click)="shiftMonth(-1)">‹</button>
        </div>
        <span class="text-sm font-semibold">{{ monthName() }} {{ viewYear() }}</span>
        <div class="flex gap-0.5">
          <button type="button" class="btn btn-ghost btn-xs btn-circle" aria-label="Next month" (click)="shiftMonth(1)">›</button>
          <button type="button" class="btn btn-ghost btn-xs btn-circle" aria-label="Next year" (click)="shiftYear(1)">»</button>
        </div>
      </div>

      <div class="grid grid-cols-7 gap-y-1 text-center">
        @for (w of weekdays; track w) {
        <span class="text-xs uppercase opacity-50">{{ w }}</span>
        }
        @for (c of cells(); track c.iso) {
        <button type="button" class="btn btn-xs mx-auto h-8 w-8 rounded-full p-0 font-normal"
          [class.btn-primary]="c.selected" [class.btn-ghost]="!c.selected"
          [class.opacity-40]="!c.inMonth" [class.opacity-30]="c.disabled"
          [class.border-primary]="c.today && !c.selected" [class.border]="c.today && !c.selected"
          [class.text-error]="c.weekend && !c.selected" [disabled]="c.disabled" (click)="select(c)">
          {{ c.day }}
        </button>
        }
      </div>

      <div class="mt-2 flex justify-end">
        <button type="button" class="btn btn-ghost btn-xs rounded-full" [disabled]="todayDisabled()" (click)="pickToday()">Today</button>
      </div>
    </div>
    }
  `,
})
export class DatePicker {
  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  value = model('');
  min = input('');
  max = input('');
  inputId = input('');
  placeholder = input('Select date');

  protected open = signal(false);
  protected alignRight = signal(false);
  protected viewYear = signal(new Date().getFullYear());
  protected viewMonth = signal(new Date().getMonth());
  protected readonly weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

  protected monthName = computed(() => MONTHS[this.viewMonth()]);
  protected label = computed(() => {
    const p = parse(this.value());
    return p ? `${pad(p.d)}.${pad(p.m + 1)}.${p.y}` : '';
  });
  protected todayDisabled = computed(() => this.isDisabled(todayIso()));

  protected cells = computed<Cell[]>(() => {
    const y = this.viewYear();
    const m = this.viewMonth();
    const offset = (new Date(y, m, 1).getDay() + 6) % 7; // week starts on Monday
    const today = todayIso();
    const selected = this.value();
    const cells: Cell[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(y, m, 1 - offset + i);
      const iso = toIso(d.getFullYear(), d.getMonth(), d.getDate());
      cells.push({
        iso,
        day: d.getDate(),
        inMonth: d.getMonth() === m,
        disabled: this.isDisabled(iso),
        today: iso === today,
        selected: iso === selected,
        weekend: d.getDay() === 0 || d.getDay() === 6,
      });
    }
    return cells;
  });

  private isDisabled(iso: string) {
    const min = this.min();
    const max = this.max();
    return (!!min && iso < min) || (!!max && iso > max);
  }

  toggle() {
    if (this.open()) {
      this.open.set(false);
      return;
    }
    const p = parse(this.value()) ?? parse(todayIso())!;
    this.viewYear.set(p.y);
    this.viewMonth.set(p.m);
    const rect = this.host.nativeElement.getBoundingClientRect();
    this.alignRight.set(rect.left + 288 > window.innerWidth);
    this.open.set(true);
  }

  shiftMonth(delta: number) {
    const total = this.viewYear() * 12 + this.viewMonth() + delta;
    this.viewYear.set(Math.floor(total / 12));
    this.viewMonth.set(((total % 12) + 12) % 12);
  }

  shiftYear(delta: number) {
    this.viewYear.update((y) => y + delta);
  }

  select(c: Cell) {
    if (c.disabled) return;
    this.value.set(c.iso);
    this.open.set(false);
  }

  pickToday() {
    if (this.todayDisabled()) return;
    this.value.set(todayIso());
    this.open.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: Event) {
    if (this.open() && !this.host.nativeElement.contains(e.target as Node)) this.open.set(false);
  }

  @HostListener('keydown.escape')
  onEscape() {
    this.open.set(false);
  }
}
