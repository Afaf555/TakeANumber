export type CsvCell = string | number | null | undefined;


export function fmtDate(value: string | null | undefined): string {
  if (!value) return '';
  const d = new Date(value);
  const day = String(d.getDate()).padStart(2, '0');
  const month = String(d.getMonth() + 1).padStart(2, '0');
  return `${day}.${month}.${d.getFullYear()}`;
}


export function fmtDateTime(value: string | null | undefined): string {
  if (!value) return '';
  const d = new Date(value);
  const hours = String(d.getHours()).padStart(2, '0');
  const minutes = String(d.getMinutes()).padStart(2, '0');
  return `${fmtDate(value)} ${hours}:${minutes}`;
}


function toCell(value: CsvCell): string {
  if (value === null || value === undefined) return '';

  if (typeof value === 'number') {
    return String(Math.round(value * 100) / 100).replace('.', ',');
  }

 
  if (value.includes(';') || value.includes('"') || value.includes('\n')) {
    return '"' + value.replace(/"/g, '""') + '"';
  }
  return value;
}

export function downloadCsv(filename: string, headers: string[], rows: CsvCell[][]) {
 
  const lines = [headers, ...rows].map((row) => row.map(toCell).join(';'));
  const content = '\uFEFF' + lines.join('\r\n');
  const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  URL.revokeObjectURL(link.href);
}