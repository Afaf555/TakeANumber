import { Component, input } from '@angular/core';

// Same header on every page: title, short description, and buttons on the right (<ng-content>)
@Component({
  selector: 'app-page-header',
  template: `
    <header class="flex flex-wrap items-end justify-between gap-4">
      <div class="flex flex-col gap-1 min-w-0">
        <h1 class="text-2xl sm:text-3xl font-bold tracking-tight">{{ title() }}</h1>
        @if (subtitle()) {
        <p class="text-sm text-base-content/60">{{ subtitle() }}</p>
        }
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <ng-content />
      </div>
    </header>
  `,
  host: { class: 'block' },
})
export class PageHeader {
  title = input.required<string>();
  subtitle = input<string>('');
}
