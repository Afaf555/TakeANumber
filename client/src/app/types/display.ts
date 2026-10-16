export type ActiveCounter={
    counterId:number;
    counterName: string;
    currentTicketNumber:number|null;
    currentTicketCode:string|null;
};
export type ServiceQueue={
    serviceId:number;
    serviceName:string;
    serving: { code: string; counterName:string}[];
    nextCodes: string[];
}