import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { EmployeeTicket } from '../../types/employee';

@Injectable({
  providedIn: 'root',
})
export class EmployeeService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + 'employee/';

  getCurrent() {
    return this.http.get<EmployeeTicket | null>(this.baseUrl + 'current');
  }

  callNext() {
    return this.http.post<EmployeeTicket>(this.baseUrl + 'call-next', {});
  }

  finish() {
    return this.http.post<EmployeeTicket>(this.baseUrl + 'finish', {});
  }
}