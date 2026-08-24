import type { NextResponse } from 'next/server';

/**
 * The single definition of session cookie names and attributes. Every set and every
 * delete goes through these helpers: a delete with mismatched attributes silently
 * no-ops and leaves the session alive, which surfaces as an unexplainable login loop.
 */
export const ACCESS_COOKIE = 'av_access';
export const REFRESH_COOKIE = 'av_refresh';

const REFRESH_TTL_SECONDS = 7 * 24 * 60 * 60;

function baseAttributes() {
  return {
    httpOnly: true as const,
    secure: process.env.NODE_ENV !== 'development',
    sameSite: 'strict' as const,
    path: '/',
  };
}

export interface TokenPair {
  accessToken: string;
  refreshToken: string;
  /** Access token lifetime in seconds, as authservice reports it. */
  expiresIn: number;
}

export function setAuthCookies(response: NextResponse, tokens: TokenPair): void {
  response.cookies.set(ACCESS_COOKIE, tokens.accessToken, {
    ...baseAttributes(),
    maxAge: tokens.expiresIn,
  });
  response.cookies.set(REFRESH_COOKIE, tokens.refreshToken, {
    ...baseAttributes(),
    maxAge: REFRESH_TTL_SECONDS,
  });
}

export function clearAuthCookies(response: NextResponse): void {
  response.cookies.set(ACCESS_COOKIE, '', { ...baseAttributes(), maxAge: 0 });
  response.cookies.set(REFRESH_COOKIE, '', { ...baseAttributes(), maxAge: 0 });
}
