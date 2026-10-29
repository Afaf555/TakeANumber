import { Component, inject, OnInit, signal } from '@angular/core';
import { AdminService } from '../../../core/services/admin-service';
import { Service } from '../../../types/tickets';
import { FormsModule } from '@angular/forms';
import { DisplayService } from '../../../core/services/display-service';
import { Subscription } from 'rxjs';

@Component({
  imports: [FormsModule],
  selector: 'app-manage-services',
  styleUrl: './manage-services.css',
  templateUrl: './manage-services.html',
})
export class ManageServices implements OnInit {
  private adminService = inject(AdminService);
  protected services = signal<Service[]>([])
  protected error = signal('');
  protected newName = '';
  protected newCode = '';
  protected confirmingId = signal<number | null>(null);
  private displayService=inject(DisplayService);
  private updateSub?:Subscription;

  ngOnInit(): void {
    this.load();
    this.updateSub=this.displayService.dataChanged$.subscribe(()=>this.load());

  
  }
  ngOnDestroy(){
    this.updateSub?.unsubscribe();
  }
  private load(){
      this.adminService.getServices().subscribe({
      next: (services) => this.services.set(services),
      error: () => this.error.set('Could not load services'),
    });
  }
  add() {
    const name = this.newName.trim();
    const code = this.newCode.trim();
    if (!name || !code) return;
    this.adminService.createService(name, code).subscribe({
      next: (service) => {
        this.services.update((list) => [...list, service]);
        this.newName = '';
        this.newCode = '';
        this.error.set('');
      },
      error: () => this.error.set('Could not create service'),
    });
  }

  askRemove(id: number) {
    this.confirmingId.set(id);
  }
  cancelRemove() {
    this.confirmingId.set(null);
  }

  remove(id: number) {
    this.adminService.deleteService(id).subscribe({
      next: () => {
        this.services.update((list) => list.filter((s) => s.id != id));
        this.confirmingId.set(null);
        this.error.set('');
      },
      error: (err) => {
        this.confirmingId.set(null);
        this.error.set(typeof err.error === 'string' ? err.error : 'Could not delete service')
      }
    })
  }
}
