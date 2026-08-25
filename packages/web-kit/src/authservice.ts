import { authBaseUrl } from './backends';
import type { TokenPair } from './cookies';

/**
 * The server-side client for this system's authservice instance. Only BFF routes
 * import it — the browser never learns the authservice address for API calls.
 */

export interface AuthserviceResult {
  status: number;
  body: unknown;
  tokens?: TokenPair;
}

async function post(path: string, payload: unknown, bearer?: string): Promise<AuthserviceResult> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (bearer) {
    headers.Authorization = `Bearer ${bearer}`;
  }

  let response: Response;
  try {
    response = await fetch(`${authBaseUrl()}${path}`, {
      method: 'POST',
      headers,
      body: JSON.stringify(payload),
      cache: 'no-store',
    });
  } catch {
    return { status: 503, body: { error: 'Identity service is unreachable.' } };
  }

  const body: unknown = await response.json().catch(() => ({}));
  const record = body as Record<string, unknown>;
  const tokens =
    response.ok && typeof record.accessToken === 'string' && typeof record.refreshToken === 'string'
      ? {
          accessToken: record.accessToken,
          refreshToken: record.refreshToken,
          expiresIn: typeof record.expiresIn === 'number' ? record.expiresIn : 3600,
        }
      : undefined;

  return { status: response.status, body, tokens };
}

export function login(email: string, password: string): Promise<AuthserviceResult> {
  return post('/api/v1/auth/login', { email, password });
}

export function register(payload: {
  email: string;
  password: string;
  acceptedTermsVersion: string;
  acceptedPrivacyVersion: string;
}): Promise<AuthserviceResult> {
  return post('/api/v1/auth/register', payload);
}

export function logout(bearer: string): Promise<AuthserviceResult> {
  return post('/api/v1/auth/logout', {}, bearer);
}

export async function consentVersions(): Promise<AuthserviceResult> {
  try {
    const response = await fetch(`${authBaseUrl()}/api/v1/auth/consents/versions`, { cache: 'no-store' });
    return { status: response.status, body: await response.json().catch(() => ({})) };
  } catch {
    return { status: 503, body: { error: 'Identity service is unreachable.' } };
  }
}

// Refresh rotation is single-use: two concurrent refreshes with the same token trip
// authservice's replay detection and revoke the whole token family. The in-flight
// map serializes refreshes per token within this server instance.
const inFlightRefreshes = new Map<string, Promise<AuthserviceResult>>();

export function refresh(refreshToken: string): Promise<AuthserviceResult> {
  const existing = inFlightRefreshes.get(refreshToken);
  if (existing) {
    return existing;
  }

  const pending = post('/api/v1/auth/refresh', { refreshToken }).finally(() => {
    inFlightRefreshes.delete(refreshToken);
  });
  inFlightRefreshes.set(refreshToken, pending);
  return pending;
}
