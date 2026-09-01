import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NextRequest } from 'next/server';
import { CHALLENGE_COOKIE } from '../src/cookies';
import { resetRateLimiter } from '../src/rate-limit';

/**
 * The two-factor step's security properties, which are all about where the
 * challenge lives rather than about TOTP itself (authservice owns that).
 */

const twoFactorLogin = vi.hoisted(() => vi.fn());
const login = vi.hoisted(() => vi.fn());

vi.mock('../src/authservice', () => ({
  twoFactorLogin,
  login,
  register: vi.fn(),
  logout: vi.fn(),
  consentVersions: vi.fn(),
  refresh: vi.fn(),
}));

const { handleLogin, handleTwoFactorLogin } = await import('../src/auth-routes');

function postJson(path: string, body: unknown, cookie?: string): NextRequest {
  return new NextRequest(`http://localhost:3000${path}`, {
    method: 'POST',
    headers: {
      'content-type': 'application/json',
      'x-forwarded-for': `203.0.113.${Math.floor(Math.random() * 200) + 1}`,
      ...(cookie ? { cookie } : {}),
    },
    body: JSON.stringify(body),
  });
}

beforeEach(() => {
  resetRateLimiter();
  twoFactorLogin.mockReset();
  login.mockReset();
});

describe('handleLogin, when the account has a second factor', () => {
  it('keeps the challenge token out of the response body', async () => {
    login.mockResolvedValue({
      status: 200,
      body: { requiresTwoFactor: true, challengeToken: 'super-secret-challenge', expiresIn: 300 },
    });

    const response = await handleLogin(postJson('/api/auth/login', { email: 'a@b.test', password: 'x' }));
    const body = await response.json();

    // The page learns only THAT a second factor is needed.
    expect(body).toEqual({ requiresTwoFactor: true });
    expect(JSON.stringify(body)).not.toContain('super-secret-challenge');
  });

  it('stores the challenge in an HttpOnly cookie instead', async () => {
    login.mockResolvedValue({
      status: 200,
      body: { requiresTwoFactor: true, challengeToken: 'super-secret-challenge', expiresIn: 300 },
    });

    const response = await handleLogin(postJson('/api/auth/login', { email: 'a@b.test', password: 'x' }));
    const cookie = response.cookies.get(CHALLENGE_COOKIE);

    expect(cookie?.value).toBe('super-secret-challenge');
    expect(cookie?.httpOnly).toBe(true);
    // authservice issues it with ExpiresIn: 300 — the cookie must not outlive it.
    expect(cookie?.maxAge).toBe(300);
  });
});

describe('handleTwoFactorLogin', () => {
  it('refuses when no challenge cookie is present', async () => {
    const response = await handleTwoFactorLogin(postJson('/api/auth/2fa', { code: '123456' }));
    expect(response.status).toBe(401);
    expect(twoFactorLogin).not.toHaveBeenCalled();
  });

  it('takes the challenge from the cookie, never from the request body', async () => {
    twoFactorLogin.mockResolvedValue({ status: 200, body: {}, tokens: { accessToken: 'a', refreshToken: 'r', expiresIn: 60 } });

    await handleTwoFactorLogin(
      postJson('/api/auth/2fa', { code: '123456', challengeToken: 'attacker-supplied' }, `${CHALLENGE_COOKIE}=from-cookie`),
    );

    // A challenge the page could supply is a challenge an attacker could supply.
    expect(twoFactorLogin).toHaveBeenCalledWith('from-cookie', { code: '123456' });
  });

  it('sends the authenticator code alone, so a recovery code is not spent needlessly', async () => {
    twoFactorLogin.mockResolvedValue({ status: 200, body: {}, tokens: { accessToken: 'a', refreshToken: 'r', expiresIn: 60 } });

    await handleTwoFactorLogin(
      postJson('/api/auth/2fa', { code: '123456', recoveryCode: 'ABCD-EFGH' }, `${CHALLENGE_COOKIE}=c`),
    );

    const [, secondFactor] = twoFactorLogin.mock.calls[0];
    expect(secondFactor).toEqual({ code: '123456' });
    expect(secondFactor).not.toHaveProperty('recoveryCode');
  });

  it('falls back to the recovery code when no authenticator code is given', async () => {
    twoFactorLogin.mockResolvedValue({ status: 200, body: {}, tokens: { accessToken: 'a', refreshToken: 'r', expiresIn: 60 } });

    await handleTwoFactorLogin(postJson('/api/auth/2fa', { recoveryCode: 'ABCD-EFGH' }, `${CHALLENGE_COOKIE}=c`));

    expect(twoFactorLogin).toHaveBeenCalledWith('c', { recoveryCode: 'ABCD-EFGH' });
  });

  it('rejects a request carrying neither code without calling authservice', async () => {
    const response = await handleTwoFactorLogin(postJson('/api/auth/2fa', {}, `${CHALLENGE_COOKIE}=c`));
    expect(response.status).toBe(400);
    expect(twoFactorLogin).not.toHaveBeenCalled();
  });

  it('sets the session and clears the challenge on success', async () => {
    twoFactorLogin.mockResolvedValue({
      status: 200,
      body: {},
      tokens: { accessToken: 'access', refreshToken: 'refresh', expiresIn: 3600 },
    });

    const response = await handleTwoFactorLogin(postJson('/api/auth/2fa', { code: '123456' }, `${CHALLENGE_COOKIE}=c`));

    expect(response.status).toBe(200);
    expect(response.cookies.get('av_access')?.value).toBe('access');
    // A spent challenge must not survive the login it completed.
    expect(response.cookies.get(CHALLENGE_COOKIE)?.maxAge).toBe(0);
  });

  it('clears a dead challenge on 401 so the user is not stuck in a loop', async () => {
    twoFactorLogin.mockResolvedValue({ status: 401, body: { error: 'Invalid or expired challenge.' } });

    const response = await handleTwoFactorLogin(postJson('/api/auth/2fa', { code: '000000' }, `${CHALLENGE_COOKIE}=stale`));

    expect(response.status).toBe(401);
    expect(response.cookies.get(CHALLENGE_COOKIE)?.maxAge).toBe(0);
  });
});
