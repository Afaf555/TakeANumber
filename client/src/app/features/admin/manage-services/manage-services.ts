import { Component, inject, OnInit, signal } from '@angular/core';
import { AdminService } from '../../../core/services/admin-service';
import { Service } from '../../../types/tickets';
import { FormsModule } from '@angular/forms';

@Component({
  imports: [FormsModule],
  selector: 'app-manage-services',
  styleUrl: './manage-services.css',
  templateUrl: './manage-services.html',
})
export class ManageServices implements OnInit {
  private adminService=inject(AdminService);
  protected services=signal<Service[]>([])
  protected error=signal('');
  protected newName='';


  ngOnInit(): void {
    this.adminService.getServices().subscribe({
      next: (services)=>this.services.set(services),
      error:()=> this.error.set('Could not load services'),
    });
  }
  add(){
    const name=this.newName.trim();
    if(!name) return;
    this.adminService.createService(name).subscribe({
      next:(service)=>{
        this.services.update((list)=>[...list,service]);
        this.newName='';
        this.error.set('');
      },
      error:()=>this.error.set('Could not create service'),
    });
  }
  remove(id:number){
    this.adminService.deleteService(id).subscribe({
      next:()=>{
        this.services.update((list)=>list.filter((s)=>s.id!=id));
        this.error.set('');
      },
      error:(err)=>
        this.error.set(typeof err.error==='string'? err.error:'Could not delete service')
    })
  }
}
