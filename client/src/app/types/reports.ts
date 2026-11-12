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
export type AbsentDay = { date: string; reason: string };
export type EmployeeActivity = {
    sessions: SessionReport[]; absentDays: AbsentDay[]; tickets: HandledTicket[]; log: ActivityEntry[];
};
export type HoursPoint = {
    periodStart: string; label: string; hours: number; sessions: number;
    tickets: number; avgServiceMinutes: number;
};
export type HourBucket = { hour: number; tickets: number; avgWaitMinutes: number };
export type WeekdayBucket = { label: string; tickets: number; avgWaitMinutes: number };
export type QueueReport = {
    tickets: number; called: number; avgWaitMinutes: number; medianWaitMinutes: number;
    maxWaitMinutes: number; byHour: HourBucket[]; byWeekday: WeekdayBucket[];
};
export type HoursReport = {
    points: HoursPoint[]; totalHours: number; avgHoursPerPeriod: number; workedDays: number;
    avgHoursPerWorkedDay: number; avgSessionHours: number; totalTickets: number; avgServiceMinutes: number;
};
export type CounterOvertime = { counterName: string; employees: number; overtimeHours: number };
export type EmployeeWorkforceRow = {
    id: number; username: string; firstLoginAt: string | null; tenureDays: number;
    expectedDays: number; workedDays: number; absentDays: number; absenteeismRate: number;
    vacationDays: number; sickDays: number; lateArrivals: number; avgLateMinutes: number;
    earlyDepartures: number; overtimeHours: number;
};
export type WorkforceReport = {
    totalEmployees: number; activeInRange: number; absenteeismRate: number;
    totalLateArrivals: number; totalEarlyDepartures: number; totalOvertimeHours: number;
    overtimeByCounter: CounterOvertime[]; employees: EmployeeWorkforceRow[];
};
export type PtoRow = {
    id: number; username: string;
    vacationAllowance: number; 
    vacationUsed: number;
    vacationScheduled: number; vacationRemaining: number;
    sickAllowance: number; sickUsed: number; sickRemaining: number;
};
export type Leave = {
    id: number; employeeId: number; username: string; type: string;
    startDate: string; endDate: string;
    status: 'Approved' | 'Pending' | 'Rejected'; note: string | null;
};
export type LeaveBalance = {
    year: number;
    vacationAllowance: number; vacationUsed: number; vacationRemaining: number;
    sickAllowance: number; sickUsed: number; sickRemaining: number;
};
export const LeaveTypeValue = { Vacation: 0, Sick: 1 } as const;
export type HeatmapCell={weekday: number;hour:number;tickets:number};
export type EmployeePhases = {
    date: string; employeeId: number; username: string;
    servingMinutes: number; idleMinutes: number; neglectMinutes: number;
    breakMinutes: number; afterHoursMinutes: number;
};
export type PhaseName = 'Session' | 'Serving' | 'Idle' | 'Neglect' | 'Break';
export type PhaseSegment = {
    phase: PhaseName; ticket: string | null;
    from: string; to: string;
    startMinute: number; endMinute: number;
};
export type PhaseTimelineDay = { date: string; sessions: PhaseSegment[]; segments: PhaseSegment[] };
export type PhaseTimeline = { openMinute: number; closeMinute: number; days: PhaseTimelineDay[] };
