import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Service, Ticket } from '../../types/tickets';

@Injectable({
  providedIn: 'root',
})
export class ClientService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + 'client/';

  getServices() {
    return this.http.get<Service[]>(this.baseUrl + 'services');
  }

  createTicket(serviceId: number) {
    return this.http.post<Ticket>(this.baseUrl + 'tickets', { serviceId });
  }

  getTicket(id: number) {
    return this.http.get<Ticket>(this.baseUrl + 'tickets/' + id);
  }
  
  
}