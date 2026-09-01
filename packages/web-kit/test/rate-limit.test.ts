import { beforeEach, describe, expect, it } from 'vitest';
import { NextRequest } from 'next/server';
import {
  AUTH_POLICY,
  clientIp,
  consume,
  enforceAuthRateLimit,
  resetRateLimiter,
  type RateLimitPolicy,
} from '../src/rate-limit';

const POLICY: RateLimitPolicy = { limit: 3, windowMs: 60_000 };

function requestFrom(headers: Record<string, string>): NextRequest {
  return new NextRequest('http://localhost:3000/api/auth/login', { headers });
}

beforeEach(() => {
  resetRateLimiter();
});

describe('consume', () => {
  it('allows up to the limit and rejects past it', () => {
    for (let i = 0; i < POLICY.limit; i += 1) {
      expect(consume('k', POLICY).allowed).toBe(true);
    }
    expect(consume('k', POLICY).allowed).toBe(false);
  });

  it('keeps partitions independent — one caller cannot exhaust another budget', () => {
    for (let i = 0; i < POLICY.limit; i += 1) {
      consume('noisy', POLICY);
    }
    expect(consume('noisy', POLICY).allowed).toBe(false);
    expect(consume('quiet', POLICY).allowed).toBe(true);
  });

  it('starts a fresh window once the old one expires', () => {
    const start = 1_000_000;
    for (let i = 0; i < POLICY.limit; i += 1) {
      consume('k', POLICY, start);
    }
    expect(consume('k', POLICY, start).allowed).toBe(false);
    expect(consume('k', POLICY, start + POLICY.windowMs + 1).allowed).toBe(true);
  });

  it('reports a positive retry-after when it rejects', () => {
    const start = 1_000_000;
    for (let i = 0; i < POLICY.limit; i += 1) {
      consume('k', POLICY, start);
    }
    const rejected = consume('k', POLICY, start + 10_000);
    expect(rejected.allowed).toBe(false);
    // 60s window, 10s elapsed — the client is told to wait out the remainder,
    // never 0 (which would invite an immediate retry).
    expect(rejected.retryAfterSeconds).toBe(50);
  });
});

describe('clientIp', () => {
  it('takes the left-most x-forwarded-for entry as the client', () => {
    expect(clientIp(requestFrom({ 'x-forwarded-for': '203.0.113.7, 10.0.0.1' }))).toBe('203.0.113.7');
  });

  it('falls back to x-real-ip, then to a constant', () => {
    expect(clientIp(requestFrom({ 'x-real-ip': '198.51.100.4' }))).toBe('198.51.100.4');
    expect(clientIp(requestFrom({}))).toBe('unknown');
  });
});

describe('enforceAuthRateLimit', () => {
  it('lets normal traffic through', () => {
    expect(enforceAuthRateLimit(requestFrom({ 'x-forwarded-for': '203.0.113.7' }))).toBeNull();
  });

  it('answers 429 with the kernel response shape once over budget', async () => {
    const headers = { 'x-forwarded-for': '203.0.113.9' };
    for (let i = 0; i < AUTH_POLICY.limit; i += 1) {
      enforceAuthRateLimit(requestFrom(headers));
    }

    const rejected = enforceAuthRateLimit(requestFrom(headers));
    expect(rejected).not.toBeNull();
    expect(rejected!.status).toBe(429);
    expect(rejected!.headers.get('Retry-After')).toBeTruthy();

    // Same body as RateLimitingExtensions so a client sees one contract
    // whether it was limited at the edge or at the service.
    const body = (await rejected!.json()) as { error: string; retryAfter: number };
    expect(body.error).toBe('Too many requests.');
    expect(body.retryAfter).toBeGreaterThan(0);
  });

  it('does not let one IP exhaust another IP budget', () => {
    for (let i = 0; i < AUTH_POLICY.limit + 1; i += 1) {
      enforceAuthRateLimit(requestFrom({ 'x-forwarded-for': '203.0.113.10' }));
    }
    expect(enforceAuthRateLimit(requestFrom({ 'x-forwarded-for': '203.0.113.11' }))).toBeNull();
  });
});
