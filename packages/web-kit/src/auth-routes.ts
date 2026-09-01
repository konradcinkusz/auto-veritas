import { NextRequest, NextResponse } from 'next/server';
import * as authservice from './authservice';
import {
  ACCESS_COOKIE,
  CHALLENGE_COOKIE,
  REFRESH_COOKIE,
  clearAuthCookies,
  clearChallengeCookie,
  setAuthCookies,
  setChallengeCookie,
} from './cookies';
import { isExpired, verifyAccessToken } from './session';
import { enforceAuthRateLimit } from './rate-limit';

/**
 * The BFF auth routes. Tokens live only in HttpOnly cookies set here; client
 * JavaScript never sees them and cannot set them (document.cookie cannot produce
 * an HttpOnly cookie — a server route is the only way).
 */

export async function handleLogin(request: NextRequest): Promise<NextResponse> {
  const limited = enforceAuthRateLimit(request);
  if (limited) return limited;

  const payload = (await request.json().catch(() => null)) as { email?: string; password?: string } | null;
  if (!payload?.email || !payload.password) {
    return NextResponse.json({ error: 'E-mail i hasło są wymagane.' }, { status: 400 });
  }

  const result = await authservice.login(payload.email, payload.password);
  if (result.tokens) {
    const response = NextResponse.json({ ok: true });
    setAuthCookies(response, result.tokens);
    return response;
  }

  // A 2FA-enabled account gets a challenge at this same 200 instead of tokens.
  // The challenge completes an authentication, so it is stored the way every
  // other such artifact here is — HttpOnly — and the page is told only THAT a
  // second factor is needed, never the token itself.
  const body = result.body as { requiresTwoFactor?: boolean; challengeToken?: string } | null;
  if (result.status === 200 && body?.requiresTwoFactor && typeof body.challengeToken === 'string') {
    const challenge = NextResponse.json({ requiresTwoFactor: true });
    setChallengeCookie(challenge, body.challengeToken);
    return challenge;
  }

  // Everything else passes through as authservice answered it (lockout,
  // unverified e-mail, rate limit) — the login page maps each to a message.
  return NextResponse.json(result.body, { status: result.status });
}

/**
 * Second step of a two-factor login. The challenge comes from the HttpOnly
 * cookie set by handleLogin, never from the request body: a challenge the page
 * could supply is a challenge an attacker could supply.
 */
export async function handleTwoFactorLogin(request: NextRequest): Promise<NextResponse> {
  const limited = enforceAuthRateLimit(request);
  if (limited) return limited;

  const challengeToken = request.cookies.get(CHALLENGE_COOKIE)?.value;
  if (!challengeToken) {
    return NextResponse.json(
      { error: 'Sesja logowania wygasła. Zaloguj się ponownie.' },
      { status: 401 },
    );
  }

  const payload = (await request.json().catch(() => null)) as
    | { code?: string; recoveryCode?: string }
    | null;
  const code = payload?.code?.trim();
  const recoveryCode = payload?.recoveryCode?.trim();
  if (!code && !recoveryCode) {
    return NextResponse.json({ error: 'Podaj kod z aplikacji lub kod odzyskiwania.' }, { status: 400 });
  }

  // Prefer the authenticator code. authservice only falls through to the
  // recovery code when `code` is absent, so sending both would spend a
  // single-use recovery code that was never needed.
  const result = await authservice.twoFactorLogin(
    challengeToken,
    code ? { code } : { recoveryCode: recoveryCode as string },
  );

  if (result.tokens) {
    const response = NextResponse.json({ ok: true });
    setAuthCookies(response, result.tokens);
    clearChallengeCookie(response);
    return response;
  }

  // A dead challenge (expired, or already spent) must not linger: leaving it
  // set turns one expired attempt into a loop the user cannot escape without
  // clearing cookies by hand.
  const failure = NextResponse.json(result.body, { status: result.status });
  if (result.status === 401) {
    clearChallengeCookie(failure);
  }
  return failure;
}

export async function handleRegister(request: NextRequest): Promise<NextResponse> {
  const limited = enforceAuthRateLimit(request);
  if (limited) return limited;

  const payload = (await request.json().catch(() => null)) as {
    email?: string;
    password?: string;
    acceptedTermsVersion?: string;
    acceptedPrivacyVersion?: string;
  } | null;
  if (!payload?.email || !payload.password || !payload.acceptedTermsVersion || !payload.acceptedPrivacyVersion) {
    return NextResponse.json({ error: 'E-mail, hasło i akceptacja regulaminów są wymagane.' }, { status: 400 });
  }

  const result = await authservice.register({
    email: payload.email,
    password: payload.password,
    acceptedTermsVersion: payload.acceptedTermsVersion,
    acceptedPrivacyVersion: payload.acceptedPrivacyVersion,
  });

  if (result.tokens) {
    const response = NextResponse.json({ ok: true });
    setAuthCookies(response, result.tokens);
    return response;
  }

  return NextResponse.json(result.body, { status: result.status });
}

export async function handleLogout(request: NextRequest): Promise<NextResponse> {
  const accessToken = request.cookies.get(ACCESS_COOKIE)?.value;
  if (accessToken) {
    // Best effort: revokes every refresh token the user holds, on all devices.
    await authservice.logout(accessToken);
  }

  const response = NextResponse.json({ ok: true });
  clearAuthCookies(response);
  return response;
}

export async function handleConsentVersions(): Promise<NextResponse> {
  const result = await authservice.consentVersions();
  return NextResponse.json(result.body, { status: result.status });
}

/**
 * Rehydrates client session state on page load — by design the client cannot read
 * the cookies back. Expired access tokens are refreshed here server-side, so a
 * returning visitor inside the refresh window signs in silently.
 */
export async function handleSession(request: NextRequest): Promise<NextResponse> {
  const accessToken = request.cookies.get(ACCESS_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;

  if (accessToken && !isExpired(accessToken)) {
    const session = await verifyAccessToken(accessToken);
    if (session) {
      return NextResponse.json({ authenticated: true, email: session.email, roles: session.roles });
    }
  }

  if (refreshToken) {
    const result = await authservice.refresh(refreshToken);
    if (result.tokens) {
      const session = await verifyAccessToken(result.tokens.accessToken);
      if (session) {
        const response = NextResponse.json({ authenticated: true, email: session.email, roles: session.roles });
        setAuthCookies(response, result.tokens);
        return response;
      }
    }
  }

  const response = NextResponse.json({ authenticated: false }, { status: 401 });
  clearAuthCookies(response);
  return response;
}
