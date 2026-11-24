import { Component, computed, input } from '@angular/core';


const ICONS: Record<string, string[]> = {
  dashboard: ['M4 4h6v8H4z', 'M14 4h6v4h-6z', 'M14 12h6v8h-6z', 'M4 16h6v4H4z'],
  queue: ['M4 6h16', 'M4 12h16', 'M4 18h10'],
  display: ['M3 5h18v12H3z', 'M8 21h8', 'M12 17v4'],
  services: ['M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8z', 'M7.5 7.5h.01'],
  counters: ['M3 21V9l9-6 9 6v12', 'M9 21v-6h6v6'],
  employees: ['M9 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M2 21c0-4 3-6 7-6s7 2 7 6', 'M17 4a4 4 0 0 1 0 8', 'M22 21c0-3-2-5-4-5.6'],
  reports: ['M4 20V10', 'M10 20V4', 'M16 20v-7', 'M22 20H2'],
  workforce: ['M3 3h7v7H3z', 'M14 3h7v7h-7z', 'M3 14h7v7H3z', 'M14 14h7v7h-7z'],
  calendar: ['M3 5h18v16H3z', 'M3 10h18', 'M8 3v4', 'M16 3v4'],
  settings: ['M12 15a3 3 0 1 0 0-6 3 3 0 0 0 0 6z', 'M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3 1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8 1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z'],
  logout: ['M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4', 'M16 17l5-5-5-5', 'M21 12H9'],
  menu: ['M4 6h16', 'M4 12h16', 'M4 18h16'],
  close: ['M6 6l12 12', 'M18 6 6 18'],
  alert: ['M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z', 'M12 9v4', 'M12 17h.01'],
  coffee: ['M17 8h1a4 4 0 0 1 0 8h-1', 'M3 8h14v9a4 4 0 0 1-4 4H7a4 4 0 0 1-4-4z', 'M6 2v2', 'M10 2v2', 'M14 2v2'],
  check: ['M20 6 9 17l-5-5'],
  x: ['M18 6 6 18', 'M6 6l12 12'],
  next: ['M5 12h14', 'M13 6l6 6-6 6'],
  back: ['M19 12H5', 'M11 18l-6-6 6-6'],
  leave: ['M3 5h18v16H3z', 'M3 10h18', 'M8 3v4', 'M16 3v4', 'M9 15l2 2 4-4'],
  user: ['M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8z', 'M4 21c0-4 4-6 8-6s8 2 8 6'],
};

@Component({
  selector: 'app-icon',
  template: `
    <svg aria-hidden="true" [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none"
      stroke="currentColor" [attr.stroke-width]="stroke()" stroke-linecap="round" stroke-linejoin="round">
      @for (d of paths(); track $index) {
      <path [attr.d]="d" />
      }
    </svg>
  `,
  host: { class: 'inline-flex shrink-0' },
})
export class Icon {
  name = input.required<string>();
  size = input(18);
  stroke = input(2);
  protected paths = computed(() => ICONS[this.name()] ?? []);
}
