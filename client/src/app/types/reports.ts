export type EmployeeReportRow = {
    id: number; username: string; counterName: string | null; sessions: number;
    hoursWorked: number; ticketsServed: number; noShows: number;
    avgServiceMinutes: number; lastLoginAt: string | null; nonWorkingHours:number; breakHours:number;
};
export type SessionReport = {
    id: number; loginAt: string; logoutAt: string | null; durationHours: number;
    isOpen: boolean; counterName: string | null; services: string | null;breakMinutes:number;
};
export type HandledTicket = {
    code: string; serviceName: string; startedAt: string; finishedAt: string | null;
    status: string; durationMinutes: number | null;
};
export type ActivityEntry = { createdAt: string; action: string; details: string | null };
export type EmployeeActivity = {
    sessions: SessionReport[]; tickets: HandledTicket[]; log: ActivityEntry[];
};
export type HoursPoint = {
    periodStart: string; label: string; hours: number; sessions: number;
    tickets: number; avgServiceMinutes: number;
};
export type HoursReport = {
    points: HoursPoint[]; totalHours: number; avgHoursPerPeriod: number; workedDays: number;
    avgHoursPerWorkedDay: number; avgSessionHours: number; totalTickets: number; avgServiceMinutes: number;
};