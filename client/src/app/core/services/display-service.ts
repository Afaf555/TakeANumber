import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActiveCounter, ServiceQueue } from '../../types/display';

@Injectable({
  providedIn: 'root',
})
export class DisplayService {
  private http = inject(HttpClient);
  private hubConnection?: HubConnection;

  counters = signal<ActiveCounter[]>([]);
  updated$ = new Subject<void>();
  dataChanged$ = new Subject<string>();
  queues = signal<ServiceQueue[]>([]);

  startConnection() {
    if (this.hubConnection) return;
    this.loadCounters();

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('CountersUpdated', (counters: ActiveCounter[]) => {
      this.counters.set(counters);
    });
    this.hubConnection.on('QueuesUpdated', (queues: ServiceQueue[]) => {
      this.queues.set(queues);
      this.updated$.next();
    });
    this.hubConnection.on('DataChanged', (action: string) => {
      this.loadCounters();
      this.dataChanged$.next(action);
    });
    this.hubConnection.onreconnected(() => {
      this.loadCounters();
      this.updated$.next();
      this.dataChanged$.next('Reconnected');
    });

    this.connect();
  }

  stopConnection() {
    this.hubConnection?.stop();
    this.hubConnection = undefined;
  }

  private connect() {
    this.hubConnection?.start().catch((err) => {
      console.log('SignalR error', err);
      setTimeout(() => this.connect(), 5000);
    });
  }

  private loadCounters() {
    this.http.get<ActiveCounter[]>(environment.apiUrl + 'display/counters').subscribe({
      next: (counters) => this.counters.set(counters),
    });
    this.http.get<ServiceQueue[]>(environment.apiUrl + 'display/queues')
      .subscribe({ next: (q) => this.queues.set(q) });
  }
}
