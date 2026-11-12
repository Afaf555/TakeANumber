export type ActiveCounter={
    counterId:number;
    counterName: string;
    currentTicketNumber:number|null;
    currentTicketCode:string|null;
    isOnBreak:boolean;
    nextCodes: string[];
    waiting: number;
};
export type ServiceQueue={
    serviceId:number;
    serviceName:string;
    serving: { code: string; counterName:string}[];
    nextCodes: string[];
    waiting: number;
    oldestWaitingSince: string | null;
}