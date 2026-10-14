import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginCreds, RegisterCreds, User } from '../../types/user';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  currentUser = signal<User | null>(this.loadUser());

  login(creds: LoginCreds) {
    return this.http.post<User>(this.baseUrl + 'account/login', creds).pipe(
      tap((user) => {
        if (user) this.setCurrentUser(user);
      })
    );
  }

  register(creds: RegisterCreds) {
    return this.http.post<User>(this.baseUrl + 'account/register', creds).pipe(
      tap((user) => {
        if (user) this.setCurrentUser(user);
      })
    );
  }

  setCurrentUser(user: User) {
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('ticketId');
    this.currentUser.set(null);
  }

  homeFor(role?: string) {
    switch (role) {
      case 'Admin': return '/manage-services';
      case 'Employee': return '/counter-panel';
      case 'Client': return '/take-ticket';
      default: return '/login';
    }
  }

  private loadUser(): User | null {
    const json = localStorage.getItem('user');
    return json ? JSON.parse(json) : null;
  }
}