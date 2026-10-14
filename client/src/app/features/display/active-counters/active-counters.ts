import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { DisplayService } from '../../../core/services/display-service';

@Component({
  selector: 'app-active-counters',
  imports: [],
  templateUrl: './active-counters.html',
  styleUrl: './active-counters.css',
})
export class ActiveCounters implements OnInit, OnDestroy {
  protected displayService = inject(DisplayService);

  ngOnInit() {
    this.displayService.startConnection();
  }

  ngOnDestroy() {
    this.displayService.stopConnection();
  }
}