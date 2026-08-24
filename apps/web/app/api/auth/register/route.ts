import type { NextRequest } from 'next/server';
import { handleRegister } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

export function POST(request: NextRequest) {
  return handleRegister(request);
}
