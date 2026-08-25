'use client';

import { useMemo, useState } from 'react';
import { fmtEUR, fmtPct, freshnessRank } from '../lib/format';
import type { FinancingOffer } from '../lib/types';
import { EstimateChip, FreshnessBadge, SortableTh, StructureChip } from './badges';

const TIN_MAX = 10;

type SortKey = 'provider' | 'structure' | 'tin' | 'tae' | 'monthly' | 'interest' | 'verified';

function sortValue(offer: FinancingOffer, key: SortKey): string | number | null {
  switch (key) {
    case 'provider':
      return offer.provider;
    case 'structure':
      return offer.repaymentStructure;
    case 'tin':
      return offer.tinPercent;
    case 'tae':
      return offer.taePercent;
    case 'monthly':
      return offer.monthlyInstallment60Eur;
    case 'interest':
      return offer.totalInterest60Eur;
    case 'verified':
      return offer.daysSinceVerification;
  }
}

const TYPE_LABELS: Record<string, string> = {
  Bank: 'Kredyt bankowy',
  Green: 'Kredyt "zielony" (ECO/EV)',
  Fintech: 'Fintech / neobank',
  Manufacturer: 'Finansowanie producenta',
  Subscription: 'Abonament / renting',
};

export default function CreditsTable({ offers }: { offers: FinancingOffer[] }) {
  const [search, setSearch] = useState('');
  const [type, setType] = useState('all');
  const [maxTin, setMaxTin] = useState(TIN_MAX);
  const [maxAge, setMaxAge] = useState('all');
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDir, setSortDir] = useState<1 | -1>(1);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    const result = offers.filter((offer) => {
      const matchesSearch =
        !term || offer.provider.toLowerCase().includes(term) || offer.bestFor.toLowerCase().includes(term);
      const matchesType = type === 'all' || offer.type === type;
      // Slider at its ceiling means "no cap"; subscriptions have no TIN and
      // always pass the rate filter.
      const matchesTin = maxTin >= TIN_MAX || offer.tinPercent == null || offer.tinPercent <= maxTin;
      const matchesAge = maxAge === 'all' || offer.daysSinceVerification <= Number(maxAge);
      return matchesSearch && matchesType && matchesTin && matchesAge;
    });

    if (sortKey) {
      result.sort((a, b) => {
        const av = sortValue(a, sortKey);
        const bv = sortValue(b, sortKey);
        if (av == null && bv == null) return 0;
        if (av == null) return 1;
        if (bv == null) return -1;
        if (typeof av === 'string') return av.localeCompare(bv as string) * sortDir;
        return (av - (bv as number)) * sortDir;
      });
    } else {
      result.sort((a, b) => {
        const rank = freshnessRank(a.rateFreshness) - freshnessRank(b.rateFreshness);
        if (rank !== 0) return rank;
        return (a.tinPercent ?? 99) - (b.tinPercent ?? 99);
      });
    }

    return result;
  }, [offers, search, type, maxTin, maxAge, sortKey, sortDir]);

  function onSort(key: string) {
    const typed = key as SortKey;
    if (sortKey === typed) {
      setSortDir(sortDir === 1 ? -1 : 1);
    } else {
      setSortKey(typed);
      setSortDir(1);
    }
  }

  function reset() {
    setSearch('');
    setType('all');
    setMaxTin(TIN_MAX);
    setMaxAge('all');
    setSortKey(null);
    setSortDir(1);
  }

  return (
    <section className="card">
      <h2>💳 Porównanie finansowania</h2>
      <p className="desc">
        Struktura spłaty jest pokazana wprost: „BALON&rdquo; oznacza niską ratę z dużą dopłatą na koniec — dokładnie to,
        co reklamy ukrywają.
      </p>

      <div className="controls">
        <div className="control-group grow">
          <label htmlFor="creditSearch">Szukaj banku / dostawcy</label>
          <input
            id="creditSearch"
            type="search"
            placeholder="np. Bankinter, Tesla, subskrypcja..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <div className="control-group">
          <label htmlFor="typeFilter">Typ finansowania</label>
          <select id="typeFilter" value={type} onChange={(event) => setType(event.target.value)}>
            <option value="all">Wszystkie</option>
            <option value="Bank">Kredyt bankowy</option>
            <option value="Green">Kredyt &quot;zielony&quot; (ECO/EV)</option>
            <option value="Fintech">Fintech / neobank</option>
            <option value="Manufacturer">Finansowanie producenta</option>
            <option value="Subscription">Abonament / renting</option>
          </select>
        </div>
        <div className="control-group">
          <label htmlFor="creditFreshness">Świeżość danych</label>
          <select id="creditFreshness" value={maxAge} onChange={(event) => setMaxAge(event.target.value)}>
            <option value="all">Wszystkie</option>
            <option value="14">Zweryfikowane ≤ 14 dni</option>
            <option value="45">Zweryfikowane ≤ 45 dni</option>
          </select>
        </div>
        <div className="control-group" style={{ minWidth: 280 }}>
          <label htmlFor="tinRange">
            TIN (max){' '}
            <span className="range-value">
              {maxTin.toFixed(1).replace('.', ',')}%{maxTin >= TIN_MAX ? ' (bez limitu)' : ''}
            </span>
          </label>
          <div className="range-wrap">
            <input
              id="tinRange"
              type="range"
              min={0}
              max={TIN_MAX}
              step={0.1}
              value={maxTin}
              onChange={(event) => setMaxTin(Number(event.target.value))}
            />
          </div>
        </div>
        <button className="reset-btn" type="button" onClick={reset}>
          ↺ Wyczyść filtry
        </button>
      </div>

      <div className="count-tag">
        Pokazano <strong>{filtered.length}</strong> z <strong>{offers.length}</strong> opcji
      </div>

      {filtered.length > 0 ? (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <SortableTh label="Dostawca" sortKey="provider" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh
                  label="Struktura"
                  sortKey="structure"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <SortableTh label="TIN" sortKey="tin" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="TAE" sortKey="tae" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <th>Okres</th>
                <th>Wpłata własna</th>
                <th>Opłaty</th>
                <SortableTh
                  label="Rata / 60 mies. (26k €)"
                  sortKey="monthly"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <SortableTh
                  label="Odsetki / 60 mies."
                  sortKey="interest"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <SortableTh
                  label="Weryfikacja"
                  sortKey="verified"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <th>Najlepsze dla</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((offer) => (
                <tr key={offer.id} className={freshnessRank(offer.rateFreshness) >= 2 ? 'degraded' : undefined}>
                  <td className="model-name">
                    {offer.provider}
                    <span className="sub">{TYPE_LABELS[offer.type] ?? offer.type}</span>
                  </td>
                  <td>
                    <StructureChip structure={offer.repaymentStructure} />
                  </td>
                  <td className="num">
                    {fmtPct(offer.tinPercent)} <EstimateChip confidence={offer.rateConfidence} />
                  </td>
                  <td className="num">{fmtPct(offer.taePercent)}</td>
                  <td>{offer.termDescription}</td>
                  <td>{offer.downPaymentDescription}</td>
                  <td>{offer.feesDescription}</td>
                  <td className="num price-fin">
                    {offer.monthlyInstallment60Eur != null ? `${fmtEUR(offer.monthlyInstallment60Eur)}/mies.` : '—'}
                  </td>
                  <td className="num gap">{fmtEUR(offer.totalInterest60Eur)}</td>
                  <td>
                    <FreshnessBadge
                      status={offer.rateFreshness}
                      days={offer.daysSinceVerification}
                      lastVerifiedAt={offer.lastVerifiedAt}
                      offerValidUntil={offer.offerValidUntil}
                      sourcePublishedAt={offer.sourcePublishedAt}
                      isExpired={offer.isExpired}
                    />
                  </td>
                  <td className="note-cell">{offer.bestFor}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="empty-state">Brak opcji finansowania spełniających wybrane kryteria.</div>
      )}

      <div className="legend">
        <span>
          💡 Zasada wcześniejszej spłaty: max 1% kwoty przy &gt;1 roku pozostałym, 0,5% przy &lt;1 roku (prawo
          hiszpańskie)
        </span>
        <span>
          🌱 Kredyt &quot;zielony&quot; = niższy TIN dla aut z etykietą ECO/CERO — zwykle 0,5–3 p.p. taniej niż kredyt
          standardowy
        </span>
      </div>
    </section>
  );
}
