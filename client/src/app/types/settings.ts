export type WorkSettings = {
  openTime: string;
  closeTime: string;
  lateGraceMinutes: number;
  earlyGraceMinutes: number;
  standardDayHours: number;
  queueAlertWaiting: number;
  queueAlertMinutes: number;
  annualLeaveDays: number;
  annualSickDays: number;
};

export const DEFAULT_SETTINGS: WorkSettings = {
  openTime: '08:00',
  closeTime: '17:00',
  lateGraceMinutes: 10,
  earlyGraceMinutes: 10,
  standardDayHours: 8,
  queueAlertWaiting: 10,
  queueAlertMinutes: 15,
  annualLeaveDays: 20,
  annualSickDays: 10,
};

export const shiftTime = (time: string, minutes: number): string => {
  const [h, m] = time.split(':').map(Number);
  const total = Math.max(0, Math.min(23 * 60 + 59, h * 60 + m + minutes));
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(Math.floor(total / 60))}:${pad(total % 60)}`;
};