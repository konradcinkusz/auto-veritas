import type { NextRequest } from 'next/server';
import { handleProxy } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

type Context = { params: Promise<{ path: string[] }> };

export function GET(request: NextRequest, context: Context) {
  return handleProxy(request, context);
}

export function POST(request: NextRequest, context: Context) {
  return handleProxy(request, context);
}

export function PUT(request: NextRequest, context: Context) {
  return handleProxy(request, context);
}

export function PATCH(request: NextRequest, context: Context) {
  return handleProxy(request, context);
}

export function DELETE(request: NextRequest, context: Context) {
  return handleProxy(request, context);
}
