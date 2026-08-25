'use client';

import { useEffect, useState } from 'react';

/**
 * A lazily-fetched, expandable panel injected as a full-width row directly
 * below the offer it belongs to. Shared by both tables — the fetch/loading/
 * error handling is identical, only the rendered columns differ per entity.
 */
export function HistoryToggle({ open, onToggle }: { open: boolean; onToggle: () => void }) {
  return (
    <button type="button" className="history-toggle" onClick={onToggle}>
      {open ? 'Ukryj historię' : 'Historia'}
    </button>
  );
}

type FetchState<T> =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'ready'; entries: T[] };

export function HistoryRow<T>({
  colSpan,
  fetchUrl,
  emptyLabel,
  renderEntries,
}: {
  colSpan: number;
  fetchUrl: string;
  emptyLabel: string;
  renderEntries: (entries: T[]) => React.ReactNode;
}) {
  const [state, setState] = useState<FetchState<T>>({ status: 'loading' });

  useEffect(() => {
    let cancelled = false;
    setState({ status: 'loading' });

    fetch(fetchUrl)
      .then((response) => (response.ok ? (response.json() as Promise<T[]>) : Promise.reject(new Error())))
      .then((entries) => {
        if (!cancelled) {
          setState({ status: 'ready', entries });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setState({ status: 'error' });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [fetchUrl]);

  return (
    <tr className="history-row">
      <td colSpan={colSpan}>
        {state.status === 'loading' && <span className="history-status">Ładowanie historii…</span>}
        {state.status === 'error' && <span className="history-status">Nie udało się pobrać historii.</span>}
        {state.status === 'ready' &&
          (state.entries.length === 0 ? (
            <span className="history-status">{emptyLabel}</span>
          ) : (
            renderEntries(state.entries)
          ))}
      </td>
    </tr>
  );
}
