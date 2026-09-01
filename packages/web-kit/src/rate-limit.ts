import { NextRequest, NextResponse } from 'next/server';
import { ACCESS_COOKIE } from './cookies';
import { verifyAccessToken } from './session';

/**
 * Rate limiting for the BFF itself.
 *
 * The two backends are already protected — authservice limits its auth
 * endpoints per IP, the OffersService limits per user — but the Next.js
 * process in front of them was not, so a hostile client could hammer the
 * server that fans every request out to both. That is the deviation this
 * closes (SERVICE-API-PATTERNS' partial-coverage failure mode).
 *
 * Response shape is deliberately identical to the kernel's
 * (`RateLimitingExtensions`): 429 with `{ error, retryAfter }` and a
 * `Retry-After` header, so a client written against the API sees one contract
 * whether the limit was hit at the edge or at the service.
 *
 * KNOWN LIMIT — this counter lives in the instance's memory. Two Fly machines
 * mean two independent budgets, and a restart forgets everything. That is
 * acceptable for what this defends against (one client hammering one process)
 * and useless against a distributed flood, which belongs at the edge/CDN
 * instead. Do not mistake this for a global quota.
 */

export interface RateLimitPolicy {
  /** Requests allowed per window, per partition. */
  limit: number;
  /** Window length in milliseconds. */
  windowMs: number;
}

/**
 * Generous: a viewer opening the dashboard fires several proxy calls, and the
 * OffersService's own per-user ceiling (200/min) is the real quota. This exists
 * to stop a flood, not to shape normal use.
 */
export const PROXY_POLICY: RateLimitPolicy = { limit: 300, windowMs: 60_000 };

/**
 * Strict: login and register are unauthenticated and credential-bearing, so
 * these partition by IP by definition. authservice already limits 20/min per
 * IP; this is the same order of magnitude, applied one hop earlier so the
 * traffic never reaches it.
 */
export const AUTH_POLICY: RateLimitPolicy = { limit: 30, windowMs: 60_000 };

interface Counter {
  count: number;
  resetAt: number;
}

const buckets = new Map<string, Counter>();

/**
 * Sweeping on write keeps the map from growing without bound under a flood of
 * distinct partitions, with no timer to leak in a serverless runtime.
 */
function sweep(now: number): void {
  if (buckets.size < 1_000) return;
  for (const [key, counter] of buckets) {
    if (counter.resetAt <= now) {
      buckets.delete(key);
    }
  }
}

export interface RateLimitResult {
  allowed: boolean;
  retryAfterSeconds: number;
}

export function consume(partition: string, policy: RateLimitPolicy, now = Date.now()): RateLimitResult {
  sweep(now);
  const existing = buckets.get(partition);

  if (!existing || existing.resetAt <= now) {
    buckets.set(partition, { count: 1, resetAt: now + policy.windowMs });
    return { allowed: true, retryAfterSeconds: 0 };
  }

  existing.count += 1;
  if (existing.count > policy.limit) {
    return { allowed: false, retryAfterSeconds: Math.max(1, Math.ceil((existing.resetAt - now) / 1000)) };
  }
  return { allowed: true, retryAfterSeconds: 0 };
}

/** Test seam — the module-level map would otherwise leak between cases. */
export function resetRateLimiter(): void {
  buckets.clear();
}

/**
 * Fly (and any sane proxy) sets x-forwarded-for; the left-most entry is the
 * client. `request.ip` is not populated on all runtimes, so it is a fallback
 * rather than the primary source.
 */
export function clientIp(request: NextRequest): string {
  const forwarded = request.headers.get('x-forwarded-for');
  if (forwarded) {
    const first = forwarded.split(',')[0]?.trim();
    if (first) return first;
  }
  return request.headers.get('x-real-ip') ?? 'unknown';
}

/**
 * The partition key, and the one security-relevant decision in this file.
 *
 * A signed-in caller is keyed by their subject so that users behind one NAT
 * do not share a budget. That subject comes from a VERIFIED token — decoding
 * the cookie without checking its signature would let anyone mint an endless
 * supply of partitions by varying `sub`, which is a bypass, not a limiter.
 * Verification is a local check against the cached JWKS, and the request is
 * about to make a network hop anyway, so the cost is noise.
 *
 * Anything that does not verify — anonymous, expired, forged — falls back to
 * the client IP, which the caller cannot choose.
 */
export async function partitionKey(request: NextRequest): Promise<string> {
  const token = request.cookies.get(ACCESS_COOKIE)?.value;
  if (token) {
    const session = await verifyAccessToken(token);
    if (session?.subject) {
      return `user:${session.subject}`;
    }
  }
  return `ip:${clientIp(request)}`;
}

/**
 * Returns a 429 response when the caller is over budget, or null to proceed.
 */
export async function enforceRateLimit(
  request: NextRequest,
  policy: RateLimitPolicy,
): Promise<NextResponse | null> {
  const { allowed, retryAfterSeconds } = consume(await partitionKey(request), policy);
  if (allowed) {
    return null;
  }

  return NextResponse.json(
    { error: 'Too many requests.', retryAfter: retryAfterSeconds },
    { status: 429, headers: { 'Retry-After': String(retryAfterSeconds) } },
  );
}

/**
 * Auth routes are unauthenticated by definition, so they key on IP directly
 * rather than paying for a token verification that cannot succeed.
 */
export function enforceAuthRateLimit(request: NextRequest): NextResponse | null {
  const { allowed, retryAfterSeconds } = consume(`auth:${clientIp(request)}`, AUTH_POLICY);
  if (allowed) {
    return null;
  }

  return NextResponse.json(
    { error: 'Too many requests.', retryAfter: retryAfterSeconds },
    { status: 429, headers: { 'Retry-After': String(retryAfterSeconds) } },
  );
}
