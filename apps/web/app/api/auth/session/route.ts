import type { NextRequest } from 'next/server';
import { handleSession } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

export function GET(request: NextRequest) {
  return handleSession(request);
}
