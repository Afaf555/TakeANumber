import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin-service';
import { Counter, Employee } from '../../../types/admin';
import { Service } from '../../../types/tickets';
import { Subscription } from 'rxjs';
import { DisplayService } from '../../../core/services/display-service';

@Component({
  selector: 'app-manage-employees',
  imports: [FormsModule],
  templateUrl: './manage-employees.html',
  styleUrl: './manage-employees.css',
})
export class ManageEmployees implements OnInit, OnDestroy {

  private adminService = inject(AdminService);
  private displayService = inject(DisplayService);
  private updateSub?: Subscription;
  protected employees = signal<Employee[]>([]);
  protected counters = signal<Counter[]>([]);
  protected services = signal<Service[]>([]);
  protected error = signal('');
  protected success = signal('');

  protected username = '';
  protected password = '';
  protected counterId: number | null = null;

  ngOnInit() {
    this.adminService.getCounters().subscribe({
      next: (counters) => this.counters.set(counters),
      error: () => this.error.set('Could not load counters'),
    });
    this.adminService.getServices().subscribe({
      next: (services) => this.services.set(services),
      error: () => this.error.set('Could not load services'),
    });
    this.loadEmployees();
    this.updateSub = this.displayService.updated$.subscribe(() => this.loadEmployees());
    this.displayService.startConnection();
  }
  ngOnDestroy() {
    this.updateSub?.unsubscribe();
    this.displayService.stopConnection();
  }
  create() {
    // if (this.counterId === null) return;

    this.adminService
      .createEmployee({
        username: this.username.trim(),
        password: this.password,
        counterId: this.counterId,
      })
      .subscribe({
        next: () => {
          this.success.set(`Employee "${this.username.trim().toLowerCase()}" created`);
          this.error.set('');
          this.username = '';
          this.password = '';
          this.counterId = null;
          this.loadEmployees();
        },
        error: (err) => {
          this.success.set('');
          this.error.set(typeof err.error === 'string' ? err.error : 'Could not create employee');
        },
      });
  }

  changeCounter(employee: Employee, counterId: number | null) {
    const request =
      counterId === null
        ? this.adminService.unassignCounter(employee.id)
        : this.adminService.assignCounter(employee.id, counterId);

    request.subscribe({
      next: () => {
        this.error.set('');
        this.loadEmployees();
      },
      error: (err) => {
        this.error.set(typeof err.error === 'string' ? err.error : 'Could not change counter');
        this.loadEmployees();
      },
    });
  }

  servicesOf(employee: Employee) {
    const counter = this.counters().find((c) => c.id === employee.counterId);
    if (!counter) return '';
    return this.services()
      .filter((s) => counter.serviceIds.includes(s.id))
      .map((s) => s.name)
      .join(', ');
  }

  private loadEmployees() {
    this.adminService.getEmployees().subscribe({
      next: (employees) => this.employees.set(employees),
      error: () => this.error.set('Could not load employees'),
    });
  }

  remove(id: number) {
    this.adminService.deleteEmployee(id).subscribe({
      next: () => {
        this.employees.update((list) => list.filter((e) => e.id !== id));
        this.error.set('');
        this.success.set('');
      },
      error: (err) => {
        this.error.set(typeof err.error === 'string' ? err.error : 'Could not delete employee');
        this.loadEmployees();
      }

    })
  }

}