import { describe, expect, it } from 'vitest';
import { NextResponse } from 'next/server';
import { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies, setAuthCookies } from '../src/cookies';

describe('auth cookies', () => {
  it('sets both cookies HttpOnly, SameSite=Strict and path=/', () => {
    const response = NextResponse.json({ ok: true });

    setAuthCookies(response, { accessToken: 'a', refreshToken: 'r', expiresIn: 3600 });

    for (const name of [ACCESS_COOKIE, REFRESH_COOKIE]) {
      const cookie = response.cookies.get(name);
      expect(cookie, name).toBeDefined();
      expect(cookie?.httpOnly, name).toBe(true);
      expect(cookie?.sameSite, name).toBe('strict');
      expect(cookie?.path, name).toBe('/');
    }
    expect(response.cookies.get(ACCESS_COOKIE)?.maxAge).toBe(3600);
  });

  it('clears with the same attributes it set with, so the delete cannot no-op', () => {
    const response = NextResponse.json({ ok: true });

    setAuthCookies(response, { accessToken: 'a', refreshToken: 'r', expiresIn: 3600 });
    clearAuthCookies(response);

    for (const name of [ACCESS_COOKIE, REFRESH_COOKIE]) {
      const cookie = response.cookies.get(name);
      expect(cookie?.value, name).toBe('');
      expect(cookie?.maxAge, name).toBe(0);
      expect(cookie?.httpOnly, name).toBe(true);
      expect(cookie?.sameSite, name).toBe('strict');
      expect(cookie?.path, name).toBe('/');
    }
  });
});
