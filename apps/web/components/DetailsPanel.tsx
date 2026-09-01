'use client';

/**
 * The per-offer drill-down: the three trust dates, the confidence marker, and
 * — the point of it — a link to the page the number was actually read from.
 *
 * Rendered as an expandable row rather than a dedicated route, matching the
 * pattern HistoryPanel already established. Every field it shows is already in
 * the list response, so unlike the history panel this needs no fetch: opening
 * it costs one render, not one request.
 *
 * `sourceUrl` is agent-supplied data. It is rendered as a link and NEVER
 * fetched server-side — the security review's A10/SSRF section asserts that
 * every server-side fetch targets a configuration-derived base, and this
 * component must not become the exception.
 */
export function DetailsToggle({ open, onToggle }: { open: boolean; onToggle: () => void }) {
  return (
    <button type="button" className="history-toggle" onClick={onToggle}>
      {open ? 'Ukryj szczegóły' : 'Szczegóły'}
    </button>
  );
}

export interface DetailField {
  label: string;
  value: React.ReactNode;
}

export function DetailsRow({
  colSpan,
  fields,
  sourceName,
  sourceUrl,
}: {
  colSpan: number;
  fields: DetailField[];
  sourceName: string | null;
  sourceUrl: string | null;
}) {
  return (
    <tr className="history-row">
      <td colSpan={colSpan}>
        <dl className="details-grid">
          {fields.map((field) => (
            <div key={field.label} className="details-item">
              <dt>{field.label}</dt>
              <dd>{field.value}</dd>
            </div>
          ))}
          <div className="details-item details-source">
            <dt>Źródło</dt>
            <dd>
              <SourceLink name={sourceName} url={sourceUrl} />
            </dd>
          </div>
        </dl>
      </td>
    </tr>
  );
}

/**
 * Three cases, all real in the data: a link with a name, a bare URL with no
 * name, and no source at all (both columns are nullable). An offer with no
 * recorded source says so plainly — claiming verification without being able
 * to point at what was verified is the failure mode this panel exists to close.
 */
function SourceLink({ name, url }: { name: string | null; url: string | null }) {
  if (!url) {
    return <span className="details-nosource">{name ?? 'brak zapisanego źródła'}</span>;
  }

  return (
    <a href={url} target="_blank" rel="noopener noreferrer nofollow" className="details-link">
      {name ?? url}
      <span aria-hidden="true"> ↗</span>
    </a>
  );
}
