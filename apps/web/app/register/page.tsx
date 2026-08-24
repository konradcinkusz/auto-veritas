'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { FormEvent, useEffect, useState } from 'react';

interface ConsentVersions {
  terms?: string;
  privacy?: string;
}

export default function RegisterPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [consents, setConsents] = useState<ConsentVersions | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    // Consent versions come from the identity service, never hardcoded: a bumped
    // version on the server would otherwise reject every registration.
    fetch('/api/auth/consents')
      .then((response) => (response.ok ? (response.json() as Promise<ConsentVersions>) : null))
      .then(setConsents)
      .catch(() => setConsents(null));
  }, []);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!accepted || !consents?.terms || !consents.privacy) {
      setError('Zaakceptuj regulamin i politykę prywatności.');
      return;
    }
    setBusy(true);
    setError(null);

    const response = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email,
        password,
        acceptedTermsVersion: consents.terms,
        acceptedPrivacyVersion: consents.privacy,
      }),
    }).catch(() => null);

    if (response?.status === 200) {
      router.push('/');
      router.refresh();
      return;
    }
    if (response?.status === 202) {
      setInfo('Konto utworzone. Potwierdź adres e-mail, klikając link z wiadomości, a następnie zaloguj się.');
      setBusy(false);
      return;
    }

    if (response) {
      const body = (await response.json().catch(() => ({}))) as { errors?: string[]; error?: string };
      const messages = body.errors?.join(' ') ?? body.error;
      setError(
        messages ??
          'Rejestracja nie powiodła się. Hasło musi mieć min. 8 znaków, wielką i małą literę, cyfrę oraz znak specjalny.',
      );
    } else {
      setError('Nie udało się połączyć z serwerem. Spróbuj ponownie.');
    }
    setBusy(false);
  }

  return (
    <div className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <h1>Załóż konto</h1>
        <p className="subtitle">Dostęp do ofert wymaga zalogowania.</p>
        <div className="field">
          <label htmlFor="email">E-mail</label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="password">Hasło</label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </div>
        <div className="consent-row">
          <input
            id="consents"
            type="checkbox"
            checked={accepted}
            onChange={(event) => setAccepted(event.target.checked)}
          />
          <label htmlFor="consents">
            Akceptuję regulamin (wersja {consents?.terms ?? '…'}) i politykę prywatności (wersja{' '}
            {consents?.privacy ?? '…'}).
          </label>
        </div>
        {error && (
          <p className="auth-error" role="alert">
            {error}
          </p>
        )}
        {info && <p className="auth-info">{info}</p>}
        <button className="primary" type="submit" disabled={busy || !consents}>
          {busy ? 'Rejestrowanie…' : 'Zarejestruj się'}
        </button>
        <p className="auth-alt">
          Masz już konto? <Link href="/login">Zaloguj się</Link>
        </p>
      </form>
    </div>
  );
}
