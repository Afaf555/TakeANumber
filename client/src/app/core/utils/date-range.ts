export function rangeError(from: string, to: string): string | null {
  if (!from || !to) return 'Please choose both dates';
  if (to < from) return 'End date cannot be before start date';
  return null;
}

export const localIso = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

export const todayIso = () => localIso(new Date());
