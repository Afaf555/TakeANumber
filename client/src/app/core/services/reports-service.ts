import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from "../../../environments/environment";
import { EmployeeActivity, EmployeeReportRow, HoursReport } from "../../types/reports";

@Injectable({providedIn:'root'})
export class ReportsService{
    private http=inject(HttpClient);
    private baseUrl=environment.apiUrl+'reports/';
    
   getEmployees(from: string, to: string) {
    return this.http.get<EmployeeReportRow[]>(this.baseUrl + 'employees', { params: { from, to } });
  }
  getActivity(id:number,from:string,to:string){
    return this.http.get<EmployeeActivity>(this.baseUrl+'employees/'+id+'/activity',{
        params:{from,to},
    })
  }
  getHours(from:string,to:string,groupBy:string,employeeId:number|null){
    let params=new HttpParams().set('from',from).set('to',to).set('groupBy',groupBy);
    if(employeeId!==null) params=params.set('employeeId',employeeId);
    return this.http.get<HoursReport>(this.baseUrl+'hours',{params});
  }
}