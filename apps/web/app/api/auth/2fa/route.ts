import type { NextRequest } from 'next/server';
import { handleTwoFactorLogin } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

export function POST(request: NextRequest) {
  return handleTwoFactorLogin(request);
}
