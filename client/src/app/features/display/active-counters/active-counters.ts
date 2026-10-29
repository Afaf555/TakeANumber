import { Component, inject } from '@angular/core';
import { DisplayService } from '../../../core/services/display-service';

@Component({
  selector: 'app-active-counters',
  imports: [],
  templateUrl: './active-counters.html',
  styleUrl: './active-counters.css',
})
export class ActiveCounters {
  protected displayService = inject(DisplayService);
}
