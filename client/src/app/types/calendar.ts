export type NonWorkingDay={
    id:number;
    date:string;
    name:string
};
export type EmployeeWork={
    employeeId: number;
    username:string;
    sessions:number;
    hours:number;
    ticketsServed:number;
};
export type NonWorkingDayWork={
    date:string;
    reason:string;
    isHoliday:boolean;
    employees:EmployeeWork[];
};