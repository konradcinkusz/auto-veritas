import { NextResponse } from 'next/server';

/**
 * Runtime client configuration. Nothing environment-specific is ever baked into
 * the bundle via NEXT_PUBLIC_* — that breaks build-once-deploy-many. The route
 * reads process.env at request time; the short cache with stale-while-revalidate
 * keeps a promoted image from serving the previous environment's values for long.
 */
export function handleConfig(): NextResponse {
  return NextResponse.json(
    {
      appEnv: process.env.APP_ENV ?? 'development',
      // Client-safe only. Backend addresses stay server-side — the browser talks
      // exclusively to this origin.
    },
    { headers: { 'Cache-Control': 'public, max-age=60, stale-while-revalidate=300' } },
  );
}
