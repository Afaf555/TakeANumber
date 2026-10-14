import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActiveCounter } from '../../types/display';

@Injectable({
  providedIn: 'root',
})
export class DisplayService {
  private http = inject(HttpClient);
  private hubConnection?: HubConnection;

  counters = signal<ActiveCounter[]>([]);
  updated$ = new Subject<void>();

  startConnection() {
    this.loadCounters();

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('CountersUpdated', (counters: ActiveCounter[]) => {
      console.log('CountersUpdated received', counters);
      this.counters.set(counters);
      this.updated$.next();
    });

    this.hubConnection.onreconnected(() => {
      this.loadCounters();
      this.updated$.next();
    });

    this.hubConnection.start().catch((err) => console.log('SignalR error', err));
  }

  stopConnection() {
    this.hubConnection?.stop();
  }

  private loadCounters() {
    this.http.get<ActiveCounter[]>(environment.apiUrl + 'display/counters').subscribe({
      next: (counters) => this.counters.set(counters),
    });
  }
}