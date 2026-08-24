import { handleConsentVersions } from '@auto-veritas/web-kit';

export const dynamic = 'force-dynamic';

export function GET() {
  return handleConsentVersions();
}
