export type Counter={
    id:number;
    name: string;
    isActive:boolean;
    serviceIds:number[];
}
export type CreateEmployee = {
  username: string;
  password: string;
  counterId: number | null;
};
export type Employee = {
  id: number;
  username: string;
  counterId: number | null;
};
export type AdminTicket = {
  id: number;
  number: number;
  serviceName: string;
  status: string;
  createdAt: string;
  counterName: string | null;
  clientUsername: string | null;
};