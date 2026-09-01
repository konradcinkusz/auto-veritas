'use client';

import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Mirrors a table's filter/sort state into the query string, so a view can be
 * refreshed, bookmarked or pasted to someone else and come back the same.
 *
 * Deliberately NOT `useSearchParams` / `router.replace`:
 *
 * - `router.replace` runs the App Router's navigation machinery on every
 *   keystroke in the search box. Filters change far too often for that.
 * - `useSearchParams` in a client component under a server page drags in a
 *   Suspense-boundary requirement at build time for state that is purely a
 *   client concern.
 *
 * `history.replaceState` writes the address bar without navigating and without
 * a re-render, which is exactly the semantics wanted here.
 *
 * It replaces rather than pushes on purpose: pushing would put one history
 * entry per keystroke between the user and the page they came from, so Back
 * would walk them through their own typing instead of leaving the dashboard.
 * The cost is that Back does not step through filter changes — a trade taken
 * knowingly.
 */

/** Values a table syncs. Everything is a string in the URL; callers parse. */
export type UrlStateValues = Record<string, string>;

function currentParams(): URLSearchParams {
  if (typeof window === 'undefined') {
    return new URLSearchParams();
  }
  return new URLSearchParams(window.location.search);
}

/**
 * Reads this table's slice of the query string once, on mount.
 *
 * Server and first client render must agree, so the initial value is always
 * the defaults; the URL is applied in an effect straight after. Reading
 * `window` during render would be a hydration mismatch.
 */
export function useUrlState(
  prefix: string,
  defaults: UrlStateValues,
): [UrlStateValues, (patch: UrlStateValues) => void, () => void] {
  const [values, setValues] = useState<UrlStateValues>(defaults);
  const defaultsRef = useRef(defaults);
  const hydrated = useRef(false);

  useEffect(() => {
    if (hydrated.current) return;
    hydrated.current = true;

    const params = currentParams();
    const fromUrl: UrlStateValues = {};
    for (const key of Object.keys(defaultsRef.current)) {
      const raw = params.get(`${prefix}${key}`);
      if (raw !== null) {
        fromUrl[key] = raw;
      }
    }
    if (Object.keys(fromUrl).length > 0) {
      setValues((previous) => ({ ...previous, ...fromUrl }));
    }
  }, [prefix]);

  const write = useCallback(
    (next: UrlStateValues) => {
      if (typeof window === 'undefined') return;
      const params = currentParams();
      for (const [key, value] of Object.entries(next)) {
        const name = `${prefix}${key}`;
        // A parameter at its default carries no information and only makes the
        // shared link harder to read, so it is dropped rather than written.
        if (value === defaultsRef.current[key]) {
          params.delete(name);
        } else {
          params.set(name, value);
        }
      }
      const query = params.toString();
      window.history.replaceState(null, '', query ? `${window.location.pathname}?${query}` : window.location.pathname);
    },
    [prefix],
  );

  const update = useCallback(
    (patch: UrlStateValues) => {
      setValues((previous) => {
        const merged = { ...previous, ...patch };
        write(merged);
        return merged;
      });
    },
    [write],
  );

  const reset = useCallback(() => {
    setValues(defaultsRef.current);
    write(defaultsRef.current);
  }, [write]);

  return [values, update, reset];
}
