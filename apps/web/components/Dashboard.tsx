'use client';

import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { fmtDate } from '../lib/format';
import type { CarOffer, FinancingOffer, FreshnessPolicy, ListResponse, Session } from '../lib/types';
import CarsTable from './CarsTable';
import CreditsTable from './CreditsTable';

export default function Dashboard() {
  const router = useRouter();
  const [session, setSession] = useState<Session | null>(null);
  const [cars, setCars] = useState<CarOffer[] | null>(null);
  const [credits, setCredits] = useState<FinancingOffer[] | null>(null);
  const [policy, setPolicy] = useState<FreshnessPolicy | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      const sessionResponse = await fetch('/api/auth/session').catch(() => null);
      if (!sessionResponse || sessionResponse.status === 401) {
        router.push('/login');
        return;
      }
      const sessionBody = (await sessionResponse.json()) as Session;

      const [carsResponse, creditsResponse, policyResponse] = await Promise.all([
        fetch('/api/proxy/offers/api/v1/car-offers?limit=100').catch(() => null),
        fetch('/api/proxy/offers/api/v1/financing-offers?limit=100').catch(() => null),
        fetch('/api/proxy/offers/api/v1/meta/freshness-policy').catch(() => null),
      ]);

      if (cancelled) {
        return;
      }
      if (!carsResponse?.ok || !creditsResponse?.ok) {
        if (carsResponse?.status === 401 || creditsResponse?.status === 401) {
          router.push('/login');
          return;
        }
        setError('Nie udało się pobrać ofert. Odśwież stronę lub spróbuj później.');
        return;
      }

      setSession(sessionBody);
      setCars(((await carsResponse.json()) as ListResponse<CarOffer>).items);
      setCredits(((await creditsResponse.json()) as ListResponse<FinancingOffer>).items);
      if (policyResponse?.ok) {
        setPolicy((await policyResponse.json()) as FreshnessPolicy);
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [router]);

  async function logout() {
    await fetch('/api/auth/logout', { method: 'POST' }).catch(() => null);
    router.push('/login');
    router.refresh();
  }

  if (error) {
    return (
      <div className="wrap">
        <div className="empty-state">{error}</div>
      </div>
    );
  }

  if (!cars || !credits) {
    return (
      <div className="wrap">
        <div className="loading">Ładowanie zweryfikowanych ofert…</div>
      </div>
    );
  }

  const newestVerification = [...cars, ...credits]
    .map((offer) => offer.lastVerifiedAt)
    .sort()
    .at(-1);

  return (
    <div className="wrap">
      <header className="page">
        <div className="topbar">
          <h1>Auto Veritas</h1>
          <div className="user-chip">
            <span>{session?.email}</span>
            <button className="ghost" type="button" onClick={logout}>
              Wyloguj
            </button>
          </div>
        </div>
        <p className="subtitle">
          Porównanie ofert samochodów i finansowania w Hiszpanii — każda wartość z datą ostatniej weryfikacji u źródła.
        </p>
        <div className="badge-row">
          <span className="badge">🚗 {cars.length} modeli</span>
          <span className="badge">💳 {credits.length} opcji finansowania</span>
          <span className="badge">📍 Region: Alicante / Madryt ZBE</span>
          <span className="badge">🇪🇸 Ceny w EUR</span>
          {newestVerification && <span className="badge">🔎 ostatnia weryfikacja: {fmtDate(newestVerification)}</span>}
        </div>
      </header>

      <CarsTable offers={cars} />
      <CreditsTable offers={credits} />

      <footer className="page">
        Progi świeżości danych
        {policy
          ? ` — ceny: ${policy.priceFreshDays}/${policy.priceWarningDays} dni, oprocentowanie: ${policy.rateFreshDays}/${policy.rateWarningDays} dni, dane techniczne: ${policy.specFreshDays}/${policy.specWarningDays} dni`
          : ''}
        . Oferty oznaczone „szacunek&rdquo; wymagają potwierdzenia u dealera. Dane wprowadza wyłącznie agent
        prowadzącego — użytkownicy mają dostęp tylko do odczytu.
      </footer>
    </div>
  );
}
