import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-alert-modal',
  template: `
    @if (message()) {
    <dialog class="modal modal-open" role="alertdialog" aria-modal="true">
      <div class="modal-box">
        <h3 class="text-lg font-semibold" [class.text-error]="type() === 'error'" [class.text-success]="type() === 'success'">
          {{ type() === 'error' ? 'Error' : 'Success' }}
        </h3>
        <p class="py-4">{{ message() }}</p>
        <div class="modal-action">
          <button class="btn btn-sm btn-primary rounded-full" type="button" autofocus (click)="closed.emit()">OK</button>
        </div>
      </div>
    </dialog>
    }
  `,
})
export class AlertModal {
  message = input<string>('');
  type = input<'error' | 'success'>('error');
  closed = output<void>();
}