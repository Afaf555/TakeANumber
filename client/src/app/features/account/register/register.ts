import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '../../../core/services/account-service';
import { RegisterCreds } from '../../../types/user';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private accountService = inject(AccountService);
  private router = inject(Router);

  protected creds: RegisterCreds = { username: '', password: '' };
  protected error = signal('');

  register() {
    this.accountService.register(this.creds).subscribe({
      next: () => this.router.navigateByUrl('/take-ticket'),
      error: (err) =>
        this.error.set(typeof err.error === 'string' ? err.error : 'Registration failed'),
    });
  }
}