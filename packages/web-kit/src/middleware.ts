import { NextRequest, NextResponse } from 'next/server';
import { ACCESS_COOKIE, REFRESH_COOKIE, clearAuthCookies } from './cookies';
import { isExpired, verifyAccessToken } from './session';

/**
 * Edge gate for pages. This is UX, not the security boundary — every API behind
 * the proxy still enforces its own authorization; what the middleware guarantees
 * is that no page renders for a request that could not possibly hold a session.
 *
 * An expired access token with a refresh cookie present is allowed through: the
 * session route and the proxy refresh server-side, so a returning visitor is not
 * bounced to the login page an hour after signing in.
 */

export interface AuthMiddlewareOptions {
  /** Exact paths (or prefixes ending the path segment) reachable without a session. */
  publicRoutes: string[];
  /** Prefixes that keep their query strings and are never bounced through login. */
  carveOuts: string[];
}

function matches(pathname: string, route: string): boolean {
  return pathname === route || pathname.startsWith(`${route}/`);
}

export function createAuthMiddleware(options: AuthMiddlewareOptions) {
  return async function middleware(request: NextRequest): Promise<NextResponse> {
    const { pathname, search } = request.nextUrl;

    if (
      options.publicRoutes.some((route) => matches(pathname, route)) ||
      options.carveOuts.some((route) => matches(pathname, route))
    ) {
      return NextResponse.next();
    }

    const accessToken = request.cookies.get(ACCESS_COOKIE)?.value;
    const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;

    const reject = (): NextResponse => {
      if (pathname.startsWith('/api/')) {
        const response = NextResponse.json({ error: 'Not signed in.' }, { status: 401 });
        clearAuthCookies(response);
        return response;
      }
      const loginUrl = new URL(`/login?redirect=${encodeURIComponent(pathname + search)}`, request.url);
      const response = NextResponse.redirect(loginUrl);
      clearAuthCookies(response);
      return response;
    };

    if (!accessToken && !refreshToken) {
      return reject();
    }

    if (accessToken && !isExpired(accessToken)) {
      const session = await verifyAccessToken(accessToken);
      if (session) {
        return NextResponse.next();
      }
      // A well-formed but unverifiable token is a forgery or a key rotation
      // artifact; either way the session is unusable.
      if (!refreshToken) {
        return reject();
      }
    }

    // Expired (or unverifiable) access token, refresh cookie present: let the
    // request through — server-side refresh happens in the BFF routes.
    return NextResponse.next();
  };
}
