import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account-service';
import { LoginCreds } from '../../../types/user';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private accountService = inject(AccountService);
  private router = inject(Router);

  protected creds: LoginCreds = { username: '', password: '' };
  protected error = signal('');

  login() {
    this.accountService.login(this.creds).subscribe({
      next: (user) => this.router.navigateByUrl(this.accountService.homeFor(user.role)),
      error: (err) => this.error.set(err.error ?? 'Login failed'),
    });
  }


}