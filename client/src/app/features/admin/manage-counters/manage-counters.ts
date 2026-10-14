import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin-service';
import { Counter } from '../../../types/admin';
import { Service } from '../../../types/tickets';

@Component({
  imports: [FormsModule],
  selector: 'app-manage-counters',
  styleUrl: './manage-counters.css',
  templateUrl: './manage-counters.html',
})
export class ManageCounters implements OnInit {
  private adminService = inject(AdminService);
  protected counters = signal<Counter[]>([]);
  protected services = signal<Service[]>([]);
  protected error = signal('');
  protected newName = '';


  ngOnInit(): void {
    this.adminService.getServices().subscribe({
      next: (services) => this.services.set(services),
      error: () => this.error.set('Could not load services')
    });
    this.adminService.getCounters().subscribe({
      next: (counters) => this.counters.set(counters),
      error: () => this.error.set('Could not load counters'),
    });
  }

  add() {
    const name = this.newName.trim();
    if (!name) return;

    this.adminService.createCounter(name).subscribe({
      next: (counter) => {
        this.counters.update((list) => [...list, counter]);
        this.newName = '';
        this.error.set('');
      },
      error: () => this.error.set('Could not create counter'),
    });
  }

  toggleService(counter: Counter, serviceId: number, checked: boolean) {
    const ids = checked
      ? [...counter.serviceIds, serviceId]
      : counter.serviceIds.filter((id) => id !== serviceId);
    this.adminService.assignServices(counter.id, ids).subscribe({
      next: (updated) => this.replace(updated),
      error: (err) => this.error.set(typeof err.error === 'string' ? err.error : 'Could not update services'),
    });
  }
  toggleActive(counter:Counter,isActive:boolean){
    this.adminService.setActive(counter.id,isActive).subscribe({
      next:()=>this.replace({...counter,isActive}),
      error:()=>this.error.set('Could not update counter'),
    });
  }

  replace(updated: Counter): void {
    this.counters.update((list)=>list.map((c)=>(c.id===updated.id?updated:c)));
  }
  remove(id:number){
    this.adminService.deleteCounter(id).subscribe({

      next:()=>{
        this.counters.update((list)=>list.filter((c)=>c.id!==id));
        this.error.set('');
      },
      error:(err)=>
        this.error.set(typeof err.error==='string'?err.error:'Could not delete counter')
    });
  }
  protected editingId:number|null=null;
  protected editName='';
  startEdit(counter:Counter){
    this.editingId=counter.id;
    this.editName=counter.name;
  }
  cancelEdit(){
    this.editingId=null;
  }
  saveEdit(counter:Counter){
    const name=this.editName.trim();
    if(!name) return;
    this.adminService.renameCounter(counter.id,name).subscribe({
      next:(updated)=>{
        this.replace(updated);
        this.editingId=null;
        this.error.set('')
      },
      error:(err)=>
        this.error.set(typeof err.error==='string'?err.error:'Could not rename counter'),
    })
  }

}
