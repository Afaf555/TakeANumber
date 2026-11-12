export type EmployeeTicket={
    id: number;
    number: number;
    serviceName: string;
    status:string;
    createdAt: string;
    startedAt: string | null;
    code:string;
};
export type EmployeeOverview = {
    counterName: string | null;
    waiting: { code: string; serviceName: string; createdAt: string }[];
    servedToday: number;
    noShowsToday: number;
    avgServiceMinutes: number;
};