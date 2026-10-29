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
  private dayTimer?: ReturnType<typeof setInterval>;

  constructor(){
    this.watchDay();
  }
  
  private today(){
    return new Date().toISOString().slice(0,10);
  }
  private watchDay(){
    clearInterval(this.dayTimer);
    if(this.currentUser()?.role!=='Employee') return;

    this.dayTimer=setInterval(()=>{
      if(localStorage.getItem('loginDay')!==this.today()){
        clearInterval(this.dayTimer);
        localStorage.removeItem('user');
        localStorage.removeItem('loginDay');
        this.currentUser.set(null);
        this.router.navigateByUrl('/login');
      }
    },30000)
  }

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
    localStorage.setItem('loginDay',this.today());
    this.currentUser.set(user);
    this.watchDay();
  }

  logout() {
    this.http.post(this.baseUrl + 'account/logout', {}).pipe(
      catchError(() => of(null))
    ).subscribe(() => {
      clearInterval(this.dayTimer);
      localStorage.removeItem('user');
      localStorage.removeItem('loginDay')
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
    if(!json) return null;

    const user:User=JSON.parse(json);
    if(user.role=='Employee' && localStorage.getItem('loginDay')!== this.today()){
      localStorage.removeItem('user');
      localStorage.removeItem('loginDay');
      return null;
    }
    return user;
  }
}