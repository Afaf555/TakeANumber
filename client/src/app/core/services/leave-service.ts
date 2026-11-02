import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Leave, LeaveBalance } from '../../types/reports';

@Injectable({ providedIn: 'root' })
export class LeaveService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl + 'leave';

  getLeaves(year: number) {
    return this.http.get<Leave[]>(this.baseUrl, { params: { year } });
  }
  createLeave(employeeId: number, type: number, startDate: string, endDate: string, note: string | null = null) {
    return this.http.post<Leave>(this.baseUrl, { employeeId, type, startDate, endDate, note });
  }
  approve(id: number) {
    return this.http.post<Leave>(this.baseUrl + '/' + id + '/approve', {});
  }
  reject(id: number) {
    return this.http.post<Leave>(this.baseUrl + '/' + id + '/reject', {});
  }
  
  getMine(year: number) {
    return this.http.get<Leave[]>(this.baseUrl + '/mine', { params: { year } });
  }
  getBalance(year: number) {
    return this.http.get<LeaveBalance>(this.baseUrl + '/mine/balance', { params: { year } });
  }
  requestLeave(type: number, startDate: string, endDate: string, note: string | null) {
    return this.http.post<Leave>(this.baseUrl + '/request', { type, startDate, endDate, note });
  }
  cancelMine(id: number) {
    return this.http.delete(this.baseUrl + '/mine/' + id);
  }
  deleteLeave(id: number) {
    return this.http.delete(this.baseUrl + '/' + id);
  }
}