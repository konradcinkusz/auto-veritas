import type { NextRequest } from 'next/server';
import { handleLogout } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

export function POST(request: NextRequest) {
  return handleLogout(request);
}
