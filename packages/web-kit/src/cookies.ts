import type { NextResponse } from 'next/server';

/**
 * The single definition of session cookie names and attributes. Every set and every
 * delete goes through these helpers: a delete with mismatched attributes silently
 * no-ops and leaves the session alive, which surfaces as an unexplainable login loop.
 */
export const ACCESS_COOKIE = 'av_access';
export const REFRESH_COOKIE = 'av_refresh';
/**
 * Holds the two-factor challenge between the password step and the code step.
 * authservice calls it "useless for anything except completing this login", but
 * it is still a bearer artifact that completes an authentication, so it lives
 * where every other one does: HttpOnly, never in reach of page scripts.
 */
export const CHALLENGE_COOKIE = 'av_2fa';

const REFRESH_TTL_SECONDS = 7 * 24 * 60 * 60;
/** authservice issues the challenge with ExpiresIn: 300. Match it, don't outlive it. */
const CHALLENGE_TTL_SECONDS = 300;

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

export function setChallengeCookie(response: NextResponse, challengeToken: string): void {
  response.cookies.set(CHALLENGE_COOKIE, challengeToken, {
    ...baseAttributes(),
    maxAge: CHALLENGE_TTL_SECONDS,
  });
}

export function clearChallengeCookie(response: NextResponse): void {
  response.cookies.set(CHALLENGE_COOKIE, '', { ...baseAttributes(), maxAge: 0 });
}

export function clearAuthCookies(response: NextResponse): void {
  response.cookies.set(ACCESS_COOKIE, '', { ...baseAttributes(), maxAge: 0 });
  response.cookies.set(REFRESH_COOKIE, '', { ...baseAttributes(), maxAge: 0 });
}
