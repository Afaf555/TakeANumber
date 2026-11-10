import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { WorkSettings } from '../../types/settings';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  get() {
    return this.http.get<WorkSettings>(this.apiUrl + 'settings');
  }

  update(settings: WorkSettings) {
    return this.http.put<WorkSettings>(this.apiUrl + 'settings', settings);
  }
}
