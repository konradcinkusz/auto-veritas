import { NextRequest, NextResponse } from 'next/server';
import * as authservice from './authservice';
import { candidates } from './backends';
import { ACCESS_COOKIE, REFRESH_COOKIE, setAuthCookies, type TokenPair } from './cookies';
import { isExpired } from './session';

/**
 * The catch-all BFF proxy: the browser has exactly one base URL, and this route
 * fronts every backend through the candidate ladder. The bearer token is injected
 * server-side from the HttpOnly cookie; responses stream through untouched.
 *
 * Timeout is generous on purpose — it must cover a scale-to-zero cold start.
 */

const ROUTING: Array<{ prefix: string; service: string }> = [
  { prefix: 'offers', service: 'offers' },
];

const UPSTREAM_TIMEOUT_MS = 120_000;

const PASS_THROUGH_HEADERS = ['content-type', 'content-disposition', 'content-length', 'cache-control'];

function resolveRoute(pathSegments: string[]): { service: string; rest: string } | null {
  const [head, ...tail] = pathSegments;
  const route = ROUTING.find((entry) => entry.prefix === head);
  if (!route || tail.length === 0) {
    return null;
  }
  return { service: route.service, rest: tail.join('/') };
}

async function freshTokens(request: NextRequest): Promise<{ bearer: string | null; rotated: TokenPair | null }> {
  const accessToken = request.cookies.get(ACCESS_COOKIE)?.value ?? null;
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value ?? null;

  if (accessToken && !isExpired(accessToken)) {
    return { bearer: accessToken, rotated: null };
  }

  if (refreshToken) {
    const result = await authservice.refresh(refreshToken);
    if (result.tokens) {
      return { bearer: result.tokens.accessToken, rotated: result.tokens };
    }
  }

  return { bearer: accessToken, rotated: null };
}

export async function handleProxy(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> },
): Promise<Response> {
  const { path } = await params;
  const route = resolveRoute(path);
  if (!route) {
    return NextResponse.json({ error: 'Unknown backend route.' }, { status: 404 });
  }

  const { bearer, rotated } = await freshTokens(request);

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), UPSTREAM_TIMEOUT_MS);
  // Bodies can only be read once; buffer here so every ladder rung can resend.
  const body = request.method === 'GET' || request.method === 'HEAD' ? undefined : await request.arrayBuffer();

  try {
    for (const base of candidates(route.service)) {
      const headers: Record<string, string> = {};
      const contentType = request.headers.get('content-type');
      if (contentType) {
        headers['Content-Type'] = contentType;
      }
      if (bearer) {
        headers.Authorization = `Bearer ${bearer}`;
      }

      const upstream = await fetch(`${base}/${route.rest}${request.nextUrl.search}`, {
        method: request.method,
        headers,
        body,
        signal: controller.signal,
        cache: 'no-store',
      }).catch((error: unknown) => {
        if (controller.signal.aborted) {
          throw error;
        }
        return null;
      });

      if (!upstream) {
        continue;
      }
      if (upstream.status === 403 && !bearer) {
        // Wrong ingress for this rung; with a bearer present a 403 is a real
        // authorization answer and must reach the caller untouched.
        continue;
      }

      const responseHeaders = new Headers();
      for (const name of PASS_THROUGH_HEADERS) {
        const value = upstream.headers.get(name);
        if (value) {
          responseHeaders.set(name, value);
        }
      }

      const response = new NextResponse(upstream.body, {
        status: upstream.status,
        headers: responseHeaders,
      });
      if (rotated) {
        setAuthCookies(response, rotated);
      }
      return response;
    }

    return NextResponse.json({ error: 'All backend candidates failed.' }, { status: 503 });
  } catch (error) {
    if (controller.signal.aborted) {
      return NextResponse.json({ error: 'Upstream timeout.' }, { status: 504 });
    }
    throw error;
  } finally {
    clearTimeout(timer);
  }
}
