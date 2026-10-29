import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { NonWorkingDay, NonWorkingDayWork } from '../../types/calendar';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getDays(year: number) {
    return this.http.get<NonWorkingDay[]>(this.apiUrl + 'calendar', { params: { year } });
  }

  addDay(date: string, name: string) {
    return this.http.post<NonWorkingDay>(this.apiUrl + 'calendar', { date, name });
  }

  removeDay(id: number) {
    return this.http.delete(this.apiUrl + 'calendar/' + id);
  }

  importYear(year: number) {
    return this.http.post<number>(this.apiUrl + 'calendar/import/' + year, null);
  }

  getWork(from: string, to: string) {
    return this.http.get<NonWorkingDayWork[]>(this.apiUrl + 'reports/non-working-days', {
      params: { from, to },
    });
  }
}