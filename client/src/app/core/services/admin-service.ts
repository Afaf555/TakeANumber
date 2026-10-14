import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Service } from '../../types/tickets';
import { AdminTicket, Counter, CreateEmployee, Employee } from '../../types/admin';

@Injectable({
    providedIn: 'root',
})
export class AdminService {
    [x: string]: any;
    private http = inject(HttpClient);
    private baseURl = environment.apiUrl + 'admin/';


    getServices() {
        return this.http.get<Service[]>(this.baseURl + 'services');
    }
    createService(name: string) {
        return this.http.post<Service>(this.baseURl + 'services', { name });
    }
    deleteService(id: number) {
        return this.http.delete(this.baseURl + 'services/' + id);
    }
    getCounters() {
        return this.http.get<Counter[]>(this.baseURl + 'counters');
    }
    createCounter(name: string) {
        return this.http.post<Counter>(this.baseURl + 'counters', { name });
    }
    assignServices(id: number, serviceIds: number[]) {
        return this.http.put<Counter>(this.baseURl + 'counters/' + id + '/services', { serviceIds });
    }
    setActive(id: number, isActive: boolean) {
        return this.http.put(this.baseURl + 'counters/' + id + '/active', null, {
            params: { isActive },
        })
    }
    createEmployee(employee: CreateEmployee) {
        return this.http.post(this.baseURl + 'employees', employee);
    }
    getEmployees() {
        return this.http.get<Employee[]>(this.baseURl + 'employees');
    }

    assignCounter(id: number, counterId: number | null) {
        return this.http.put(this.baseURl + 'employees/' + id + '/counter', { counterId });
    }
    deleteCounter(id: number) {
        return this.http.delete(this.baseURl + 'counters/' + id);
    }
    deleteEmployee(id: number) {
        return this.http.delete(this.baseURl + 'employees/' + id);
    }
    renameCounter(id: number, name: string) {
        return this.http.put<Counter>(this.baseURl + 'counters/' + id + '/name', { name });
    }
    unassignCounter(id: number) {
        return this.http.delete(this.baseURl + 'employees/' + id + '/counter');
    }
    getTickets() {
        return this.http.get<AdminTicket[]>(this.baseURl + 'tickets');
    }
}