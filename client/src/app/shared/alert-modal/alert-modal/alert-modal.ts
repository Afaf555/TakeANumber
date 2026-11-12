import { Component, effect, input, OnDestroy, output } from '@angular/core';

// Messages after an action, shown as a toast in the bottom right corner.
// Success closes by itself after 4 seconds, an error stays until it is closed.
@Component({
  selector: 'app-alert-modal',
  template: `
    @if (message()) {
    <div class="fixed bottom-5 right-5 left-5 sm:left-auto z-[60] sm:w-96" [attr.role]="type() === 'error' ? 'alert' : 'status'">
      <div class="flex items-start gap-3 rounded-2xl border bg-base-100 px-4 py-3.5 shadow-xl"
        [class.border-error/30]="type() === 'error'" [class.border-success/30]="type() === 'success'">
        <span class="mt-0.5 w-6 h-6 shrink-0 rounded-full grid place-items-center text-xs font-bold"
          [class.bg-error/10]="type() === 'error'" [class.text-error]="type() === 'error'"
          [class.bg-success/10]="type() === 'success'" [class.text-success]="type() === 'success'">
          {{ type() === 'error' ? '!' : '✓' }}
        </span>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-semibold">{{ type() === 'error' ? 'Something went wrong' : 'Done' }}</p>
          <p class="text-sm text-base-content/70 break-words">{{ message() }}</p>
        </div>
        <button type="button" class="btn btn-ghost btn-xs btn-square" aria-label="Close" (click)="closed.emit()">✕</button>
      </div>
    </div>
    }
  `,
  host: { class: 'contents' }, // takes no space in the page layout
})
export class AlertModal implements OnDestroy {
  message = input<string>('');
  type = input<'error' | 'success'>('error');
  closed = output<void>();
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    effect(() => {
      clearTimeout(this.timer);
      if (this.message() && this.type() === 'success') {
        this.timer = setTimeout(() => this.closed.emit(), 4000);
      }
    });
  }

  ngOnDestroy() {
    clearTimeout(this.timer);
  }
}
