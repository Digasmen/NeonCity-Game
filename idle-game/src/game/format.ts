const SUFFIXES = ['', 'K', 'M', 'B', 'T', 'Qa', 'Qi', 'Sx', 'Sp', 'Oc', 'No', 'Dc'];

/** Formata números grandes: 1234 -> 1.23K, 5e9 -> 5.00B. */
export function fmt(n: number): string {
  if (!isFinite(n)) return '∞';
  if (n < 0) return '-' + fmt(-n);
  if (n < 1000) {
    if (n < 10 && n % 1 !== 0) return (Math.floor(n * 10) / 10).toFixed(1);
    return Math.floor(n).toString();
  }
  const e = Math.floor(Math.log10(n) / 3);
  if (e >= SUFFIXES.length) return n.toExponential(2).replace('+', '');
  const v = n / Math.pow(1000, e);
  const digits = v >= 100 ? 0 : v >= 10 ? 1 : 2;
  const p = Math.pow(10, digits);
  return (Math.floor(v * p) / p).toFixed(digits) + SUFFIXES[e];
}

/** Segundos -> "1:05" ou "2h 03m". */
export function fmtTime(sec: number): string {
  sec = Math.max(0, Math.ceil(sec));
  if (sec >= 3600) {
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    return `${h}h ${String(m).padStart(2, '0')}m`;
  }
  const m = Math.floor(sec / 60);
  return `${m}:${String(sec % 60).padStart(2, '0')}`;
}
