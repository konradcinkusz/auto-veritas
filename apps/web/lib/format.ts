import type { FreshnessStatus } from './types';

export const fmtEUR = (value: number | null | undefined): string =>
  value == null ? '—' : `${value.toLocaleString('pl-PL')} €`;

export const fmtPct = (value: number | null | undefined): string =>
  value == null ? '—' : `${value.toFixed(2).replace('.', ',')}%`;

// Server-authored instants are formatted in UTC: a seller deadline of
// 31.08 23:59 Spanish time is stored as its UTC instant, and rendering it in
// the viewer's local zone would show "ważna do 1.09" east of the market.
export const fmtDate = (iso: string | null | undefined): string =>
  iso ? new Date(iso).toLocaleDateString('pl-PL', { timeZone: 'UTC' }) : '—';

export function relativeDays(days: number): string {
  if (days === 0) return 'dzisiaj';
  if (days === 1) return 'wczoraj';
  return `${days} dni temu`;
}

/** Lower ranks sort first: stale data is degraded, never hidden. */
export function freshnessRank(status: FreshnessStatus): number {
  switch (status) {
    case 'Fresh':
      return 0;
    case 'Warning':
      return 1;
    case 'Stale':
      return 2;
    case 'Expired':
      return 3;
  }
}

export function freshnessLabel(status: FreshnessStatus, days: number): string {
  switch (status) {
    case 'Fresh':
      return `zweryfikowano ${relativeDays(days)}`;
    case 'Warning':
      return `zweryfikowano ${relativeDays(days)}`;
    case 'Stale':
      return `dane sprzed ${days} dni — zweryfikuj u dealera`;
    case 'Expired':
      return 'oferta wygasła';
  }
}
