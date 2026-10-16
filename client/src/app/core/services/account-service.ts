import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { tap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginCreds, RegisterCreds, User } from '../../types/user';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;
  currentUser = signal<User | null>(this.loadUser());
  private router = inject(Router);


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
    this.http.post(this.baseUrl + 'account/logout', {}).pipe(
      catchError(() => of(null))
    ).subscribe(() => {
      localStorage.removeItem('user');
      localStorage.removeItem('ticketId');
      this.currentUser.set(null);
      this.router.navigateByUrl('/take-ticket');
    })
  }

  homeFor(role?: string) {
    switch (role) {
      case 'Admin': return '/manage-services';
      case 'Employee': return '/counter-panel';
      default: return '/take-ticket';
    }
  }

  private loadUser(): User | null {
    const json = localStorage.getItem('user');
    return json ? JSON.parse(json) : null;
  }
}