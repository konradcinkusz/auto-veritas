import type { Confidence, DgtLabel, FreshnessStatus, RepaymentStructure } from '../lib/types';
import { fmtDate, freshnessLabel } from '../lib/format';

export function DgtChip({ label }: { label: DgtLabel }) {
  const className = label === 'Cero' ? 'tag tag-cero' : label === 'Eco' ? 'tag tag-eco' : 'tag';
  return <span className={className}>{label === 'Cero' ? 'CERO' : label.toUpperCase()}</span>;
}

export function StructureChip({ structure }: { structure: RepaymentStructure }) {
  switch (structure) {
    case 'Balloon':
      return (
        <span className="tag tag-balloon" title="Niska rata + duża dopłata na koniec — sprawdź ratę końcową!">
          BALON
        </span>
      );
    case 'Linear':
      return (
        <span className="tag tag-linear" title="Spłata liniowa — auto w 100% Twoje po ostatniej racie">
          liniowa
        </span>
      );
    case 'Subscription':
      return <span className="tag tag-subscription">abonament</span>;
    default:
      return <span className="tag">?</span>;
  }
}

export function EstimateChip({ confidence }: { confidence: Confidence }) {
  if (confidence !== 'Estimated') {
    return null;
  }
  return (
    <span className="tag tag-estimate" title="Wartość szacunkowa — do potwierdzenia u dealera/banku">
      szacunek
    </span>
  );
}

export function FreshnessBadge({
  status,
  days,
  lastVerifiedAt,
  offerValidUntil,
  sourcePublishedAt,
  isExpired,
}: {
  status: FreshnessStatus;
  days: number;
  lastVerifiedAt: string;
  offerValidUntil: string | null;
  sourcePublishedAt?: string | null;
  isExpired: boolean;
}) {
  // The source's own publication date is the third headline date; showing it
  // only when it differs from the verification date keeps rows quiet for
  // checked-live sources while exposing older rate tables and articles.
  const publishedDiffers =
    sourcePublishedAt != null && fmtDate(sourcePublishedAt) !== fmtDate(lastVerifiedAt);
  return (
    <div>
      <span className={`fresh-badge fresh-${status}`}>
        <span className="dot" />
        {freshnessLabel(status, days)}
      </span>
      <span className="verify-date">sprawdzono {fmtDate(lastVerifiedAt)}</span>
      {publishedDiffers && <span className="verify-date">źródło z {fmtDate(sourcePublishedAt)}</span>}
      {offerValidUntil && (
        <span className={`valid-until${isExpired ? ' expired' : ''}`}>
          {isExpired ? 'wygasła ' : 'ważna do '}
          {fmtDate(offerValidUntil)}
        </span>
      )}
    </div>
  );
}

export function SortableTh({
  label,
  sortKey,
  activeKey,
  direction,
  onSort,
}: {
  label: string;
  sortKey: string;
  activeKey: string | null;
  direction: 1 | -1;
  onSort: (key: string) => void;
}) {
  const active = activeKey === sortKey;
  return (
    <th
      scope="col"
      className={active ? 'sorted' : undefined}
      aria-sort={active ? (direction === 1 ? 'ascending' : 'descending') : undefined}
    >
      <button type="button" className="th-sort" onClick={() => onSort(sortKey)}>
        {label}
        {active && (
          <span className="arrow" aria-hidden="true">
            {direction === 1 ? '▲' : '▼'}
          </span>
        )}
      </button>
    </th>
  );
}
