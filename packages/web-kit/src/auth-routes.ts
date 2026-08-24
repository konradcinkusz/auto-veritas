import { NextRequest, NextResponse } from 'next/server';
import * as authservice from './authservice';
import { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies, setAuthCookies } from './cookies';
import { isExpired, verifyAccessToken } from './session';

/**
 * The BFF auth routes. Tokens live only in HttpOnly cookies set here; client
 * JavaScript never sees them and cannot set them (document.cookie cannot produce
 * an HttpOnly cookie — a server route is the only way).
 */

export async function handleLogin(request: NextRequest): Promise<NextResponse> {
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

  // Pass through the shape authservice answered with (2FA challenge, lockout,
  // unverified e-mail, rate limit) — the login page maps each to a message.
  return NextResponse.json(result.body, { status: result.status });
}

export async function handleRegister(request: NextRequest): Promise<NextResponse> {
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
