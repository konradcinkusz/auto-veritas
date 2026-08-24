// A dedicated health endpoint: a SPA fallback answers 200 for anything, so a
// platform check pointed at '/' would pass on a white screen.
export const dynamic = 'force-dynamic';

export function GET() {
  return Response.json({ status: 'Healthy', service: 'auto-veritas-web' });
}
