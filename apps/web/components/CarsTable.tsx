'use client';

import { Fragment, useMemo, useState } from 'react';
import { fmtDate, fmtEUR, freshnessRank } from '../lib/format';
import type { CarOffer, CarOfferHistoryEntry } from '../lib/types';
import { DgtChip, EstimateChip, FreshnessBadge, SortableTh } from './badges';
import { HistoryRow, HistoryToggle } from './HistoryPanel';

const CAR_TABLE_COLUMNS = 10;

function reliabilityClass(score: number | null): string {
  if (score == null) return 'rel-unproven';
  if (score >= 85) return 'rel-high';
  if (score >= 70) return 'rel-mid';
  return 'rel-unproven';
}

const PRICE_MIN = 15000;
const PRICE_MAX = 45000;

type SortKey = 'name' | 'label' | 'cv' | 'cash' | 'fin' | 'gap' | 'reliability' | 'boot' | 'verified';

function sortValue(offer: CarOffer, key: SortKey): string | number | null {
  switch (key) {
    case 'name':
      return offer.name;
    case 'label':
      return offer.dgtLabel;
    case 'cv':
      return offer.powerCv;
    case 'cash':
      return offer.cashPriceEur;
    case 'fin':
      return offer.financedPriceEur;
    case 'gap':
      return offer.priceGapEur;
    case 'reliability':
      return offer.reliabilityScore;
    case 'boot':
      return offer.bootLiters;
    case 'verified':
      return offer.daysSinceVerification;
  }
}

export default function CarsTable({ offers }: { offers: CarOffer[] }) {
  const [search, setSearch] = useState('');
  const [label, setLabel] = useState('all');
  const [maxPrice, setMaxPrice] = useState(PRICE_MAX);
  const [maxAge, setMaxAge] = useState('all');
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortDir, setSortDir] = useState<1 | -1>(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    const result = offers.filter((offer) => {
      const matchesSearch =
        !term ||
        offer.name.toLowerCase().includes(term) ||
        offer.variant.toLowerCase().includes(term) ||
        (offer.notes ?? '').toLowerCase().includes(term);
      const matchesLabel = label === 'all' || offer.dgtLabel === label;
      // Slider at its ceiling means "no cap" — otherwise offers above the
      // hard-coded maximum would be invisible with filters untouched. Under a
      // real cap an offer passes when EITHER known price fits; unpriced offers
      // drop out of an explicitly capped view.
      const matchesPrice =
        maxPrice >= PRICE_MAX ||
        (offer.cashPriceEur != null && offer.cashPriceEur <= maxPrice) ||
        (offer.financedPriceEur != null && offer.financedPriceEur <= maxPrice);
      const matchesAge = maxAge === 'all' || offer.daysSinceVerification <= Number(maxAge);
      return matchesSearch && matchesLabel && matchesPrice && matchesAge;
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
      // Default order is the trust order: stale and expired data is degraded to
      // the bottom, never hidden — a dead price behind a fresh-looking row is
      // exactly what this product exists to prevent.
      result.sort((a, b) => {
        const rank = freshnessRank(a.priceFreshness) - freshnessRank(b.priceFreshness);
        if (rank !== 0) return rank;
        // Priceless offers sink to the bottom of their freshness band instead
        // of masquerading as the cheapest.
        return (
          (a.cashPriceEur ?? a.financedPriceEur ?? Number.POSITIVE_INFINITY) -
          (b.cashPriceEur ?? b.financedPriceEur ?? Number.POSITIVE_INFINITY)
        );
      });
    }

    return result;
  }, [offers, search, label, maxPrice, maxAge, sortKey, sortDir]);

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
    setLabel('all');
    setMaxPrice(PRICE_MAX);
    setMaxAge('all');
    setSortKey(null);
    setSortDir(1);
  }

  return (
    <section className="card">
      <h2>🚘 Porównanie modeli</h2>
      <p className="desc">
        Filtruj po cenie gotówkowej, etykiecie DGT lub świeżości danych. Kliknij nagłówek kolumny, aby posortować.
      </p>

      <div className="controls">
        <div className="control-group grow">
          <label htmlFor="carSearch">Szukaj modelu / marki</label>
          <input
            id="carSearch"
            type="search"
            placeholder="np. BYD, Toyota, HEV..."
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        <div className="control-group">
          <label htmlFor="labelFilter">Etykieta DGT</label>
          <select id="labelFilter" value={label} onChange={(event) => setLabel(event.target.value)}>
            <option value="all">Wszystkie</option>
            <option value="Cero">0 / CERO</option>
            <option value="Eco">ECO</option>
          </select>
        </div>
        <div className="control-group">
          <label htmlFor="carFreshness">Świeżość danych</label>
          <select id="carFreshness" value={maxAge} onChange={(event) => setMaxAge(event.target.value)}>
            <option value="all">Wszystkie</option>
            <option value="7">Zweryfikowane ≤ 7 dni</option>
            <option value="14">Zweryfikowane ≤ 14 dni</option>
            <option value="30">Zweryfikowane ≤ 30 dni</option>
          </select>
        </div>
        <div className="control-group" style={{ minWidth: 280 }}>
          <label htmlFor="priceRange">
            Cena (max){' '}
            <span className="range-value">
              {maxPrice.toLocaleString('pl-PL')} €{maxPrice >= PRICE_MAX ? ' (bez limitu)' : ''}
            </span>
          </label>
          <div className="range-wrap">
            <input
              id="priceRange"
              type="range"
              min={PRICE_MIN}
              max={PRICE_MAX}
              step={500}
              value={maxPrice}
              onChange={(event) => setMaxPrice(Number(event.target.value))}
            />
          </div>
        </div>
        <button className="reset-btn" type="button" onClick={reset}>
          ↺ Wyczyść filtry
        </button>
      </div>

      <div className="count-tag">
        Pokazano <strong>{filtered.length}</strong> z <strong>{offers.length}</strong> modeli
      </div>

      {filtered.length > 0 ? (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <SortableTh label="Model" sortKey="name" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="Etykieta" sortKey="label" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="KM" sortKey="cv" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="Cena gotówkowa" sortKey="cash" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="Cena finansowana" sortKey="fin" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh label="Różnica" sortKey="gap" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh
                  label="Niezawodność"
                  sortKey="reliability"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <SortableTh label="Bagażnik" sortKey="boot" activeKey={sortKey} direction={sortDir} onSort={onSort} />
                <SortableTh
                  label="Weryfikacja"
                  sortKey="verified"
                  activeKey={sortKey}
                  direction={sortDir}
                  onSort={onSort}
                />
                <th>Uwagi</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((offer) => {
                const historyOpen = expandedId === offer.id;
                return (
                  <Fragment key={offer.id}>
                    <tr className={freshnessRank(offer.priceFreshness) >= 2 ? 'degraded' : undefined}>
                      <td className="model-name">
                        {offer.name}
                        <span className="sub">{offer.variant}</span>
                      </td>
                      <td>
                        <DgtChip label={offer.dgtLabel} />
                      </td>
                      <td className="num">{offer.powerCv}</td>
                      <td className="num price-cash">
                        {fmtEUR(offer.cashPriceEur)} <EstimateChip confidence={offer.priceConfidence} />
                      </td>
                      <td className="num price-fin">{fmtEUR(offer.financedPriceEur)}</td>
                      <td className="num gap">{offer.priceGapEur != null ? `~${fmtEUR(offer.priceGapEur)}` : '—'}</td>
                      <td className={`num ${reliabilityClass(offer.reliabilityScore)}`}>
                        {offer.reliabilityScore != null
                          ? `${offer.reliabilityText ?? ''} (${offer.reliabilityScore}/100)`
                          : '—'}
                      </td>
                      <td className="num">{offer.bootLiters != null ? `${offer.bootLiters} L` : '—'}</td>
                      <td>
                        <FreshnessBadge
                          status={offer.priceFreshness}
                          days={offer.daysSinceVerification}
                          lastVerifiedAt={offer.lastVerifiedAt}
                          offerValidUntil={offer.offerValidUntil}
                          sourcePublishedAt={offer.sourcePublishedAt}
                          isExpired={offer.isExpired}
                        />
                        <HistoryToggle
                          open={historyOpen}
                          onToggle={() => setExpandedId(historyOpen ? null : offer.id)}
                        />
                      </td>
                      <td className="note-cell">{offer.notes}</td>
                    </tr>
                    {historyOpen && (
                      <HistoryRow<CarOfferHistoryEntry>
                        colSpan={CAR_TABLE_COLUMNS}
                        fetchUrl={`/api/proxy/offers/api/v1/car-offers/${offer.id}/history`}
                        emptyLabel="Brak wcześniejszych zmian — to pierwsza zapisana wersja."
                        renderEntries={(entries) => (
                          <table className="history-table">
                            <thead>
                              <tr>
                                <th>Data zmiany</th>
                                <th>Cena gotówkowa</th>
                                <th>Cena finansowana</th>
                                <th>Zweryfikowano</th>
                                <th>Zmienił</th>
                              </tr>
                            </thead>
                            <tbody>
                              {entries.map((entry) => (
                                <tr key={entry.id}>
                                  <td>{fmtDate(entry.recordedAt)}</td>
                                  <td className="num">{fmtEUR(entry.cashPriceEur)}</td>
                                  <td className="num">{fmtEUR(entry.financedPriceEur)}</td>
                                  <td>{fmtDate(entry.lastVerifiedAt)}</td>
                                  <td>{entry.changedByEmail ?? '—'}</td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        )}
                      />
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="empty-state">Brak modeli spełniających wybrane kryteria.</div>
      )}

      <div className="legend">
        <span>
          <span className="dot" style={{ background: 'var(--good)' }} />0 / CERO — pełny dostęp do centrum Madrytu,
          dowolna godzina
        </span>
        <span>
          <span className="dot" style={{ background: 'var(--accent-2)' }} />
          ECO — pełny dostęp do ZBE, ograniczenia tylko w wybranych strefach specjalnych
        </span>
      </div>
    </section>
  );
}
