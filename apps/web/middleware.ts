import { createAuthMiddleware } from '@auto-veritas/web-kit/middleware';

// Pages are gated at the edge; the services behind the proxy remain the security
// boundary. /api/auth and /api/config must stay public — they are how a session
// starts and how the client learns its runtime configuration.
export default createAuthMiddleware({
  publicRoutes: ['/login', '/register', '/healthz', '/api/auth', '/api/config'],
  carveOuts: [],
});

export const config = {
  matcher: ['/((?!_next/static|_next/image|favicon.ico).*)'],
};
