'use client';

import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { FormEvent, Suspense, useState } from 'react';

function LoginForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // Set once the password step returns a challenge. The challenge token itself
  // never reaches this component — it lives in an HttpOnly cookie the BFF set,
  // so all this flag says is "ask for the second factor".
  const [awaitingSecondFactor, setAwaitingSecondFactor] = useState(false);
  const [code, setCode] = useState('');
  const [useRecoveryCode, setUseRecoveryCode] = useState(false);

  function goToDashboard() {
    const redirect = searchParams.get('redirect');
    // Same-origin paths only: '//evil.com' and '/\evil.com' are protocol-relative
    // URLs the browser would happily leave the site for.
    router.push(redirect && /^\/(?![/\\])/.test(redirect) ? redirect : '/');
    router.refresh();
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    }).catch(() => null);

    if (response?.ok) {
      const body = (await response.json()) as { ok?: boolean; requiresTwoFactor?: boolean };
      if (body.requiresTwoFactor) {
        setAwaitingSecondFactor(true);
        setBusy(false);
        return;
      }
      goToDashboard();
      return;
    }

    if (response) {
      const body = (await response.json().catch(() => ({}))) as {
        lockedOut?: boolean;
        emailVerificationRequired?: boolean;
        retryAfter?: number;
      };
      if (body.lockedOut) {
        setError('Konto tymczasowo zablokowane po zbyt wielu próbach. Spróbuj ponownie za kilka minut.');
      } else if (body.emailVerificationRequired) {
        setError('Adres e-mail nie został jeszcze potwierdzony. Sprawdź skrzynkę.');
      } else if (response.status === 429) {
        setError('Zbyt wiele prób logowania. Odczekaj chwilę.');
      } else if (response.status >= 500) {
        // A 5xx is an outage, not bad credentials — saying "wrong password"
        // for a down identity service is a lie users act on.
        setError('Nie udało się połączyć z serwerem. Spróbuj ponownie.');
      } else {
        // Deliberately generic: the UI must not reveal whether the account exists.
        setError('Nieprawidłowy e-mail lub hasło.');
      }
    } else {
      setError('Nie udało się połączyć z serwerem. Spróbuj ponownie.');
    }
    setBusy(false);
  }

  async function submitSecondFactor(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);

    const response = await fetch('/api/auth/2fa', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(useRecoveryCode ? { recoveryCode: code } : { code }),
    }).catch(() => null);

    if (response?.ok) {
      goToDashboard();
      return;
    }

    if (response?.status === 401) {
      // Either the code was wrong or the 5-minute challenge lapsed. The BFF
      // clears the dead challenge cookie on 401, so the only honest next step
      // is starting over rather than letting the user retype into nothing.
      setAwaitingSecondFactor(false);
      setCode('');
      setError('Kod jest nieprawidłowy lub sesja logowania wygasła. Zaloguj się ponownie.');
    } else if (response?.status === 429) {
      setError('Zbyt wiele prób. Odczekaj chwilę.');
    } else {
      setError('Nie udało się potwierdzić kodu. Spróbuj ponownie.');
    }
    setBusy(false);
  }

  if (awaitingSecondFactor) {
    return (
      <div className="auth-wrap">
        <form className="auth-card" onSubmit={submitSecondFactor}>
          <h1>Auto Veritas</h1>
          <p className="subtitle">
            {useRecoveryCode
              ? 'Podaj jeden z zapisanych kodów odzyskiwania.'
              : 'Podaj kod z aplikacji uwierzytelniającej.'}
          </p>
          <div className="field">
            <label htmlFor="code">{useRecoveryCode ? 'Kod odzyskiwania' : 'Kod weryfikacyjny'}</label>
            <input
              id="code"
              type="text"
              inputMode={useRecoveryCode ? 'text' : 'numeric'}
              autoComplete="one-time-code"
              autoFocus
              required
              value={code}
              onChange={(event) => setCode(event.target.value)}
            />
          </div>
          {error && (
            <p className="auth-error" role="alert">
              {error}
            </p>
          )}
          <button className="primary" type="submit" disabled={busy}>
            {busy ? 'Sprawdzanie…' : 'Potwierdź'}
          </button>
          <p className="auth-alt">
            <button
              className="linklike"
              type="button"
              onClick={() => {
                setUseRecoveryCode(!useRecoveryCode);
                setCode('');
                setError(null);
              }}
            >
              {useRecoveryCode ? 'Użyj kodu z aplikacji' : 'Nie mam dostępu do aplikacji'}
            </button>
          </p>
        </form>
      </div>
    );
  }

  return (
    <div className="auth-wrap">
      <form className="auth-card" onSubmit={submit}>
        <h1>Auto Veritas</h1>
        <p className="subtitle">Zaloguj się, aby zobaczyć zweryfikowane oferty.</p>
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
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </div>
        {error && (
          <p className="auth-error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" type="submit" disabled={busy}>
          {busy ? 'Logowanie…' : 'Zaloguj się'}
        </button>
        <p className="auth-alt">
          Nie masz konta? <Link href="/register">Zarejestruj się</Link>
        </p>
      </form>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginForm />
    </Suspense>
  );
}
