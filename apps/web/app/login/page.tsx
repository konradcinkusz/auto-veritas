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
        setError('To konto ma włączone uwierzytelnianie dwuskładnikowe, którego ta aplikacja jeszcze nie obsługuje.');
        setBusy(false);
        return;
      }
      const redirect = searchParams.get('redirect');
      router.push(redirect && redirect.startsWith('/') ? redirect : '/');
      router.refresh();
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
      } else {
        // Deliberately generic: the UI must not reveal whether the account exists.
        setError('Nieprawidłowy e-mail lub hasło.');
      }
    } else {
      setError('Nie udało się połączyć z serwerem. Spróbuj ponownie.');
    }
    setBusy(false);
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
